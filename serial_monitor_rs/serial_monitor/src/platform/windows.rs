//! Windows implementation of serial port monitoring via DLL injection.
//!
//! Architecture:
//!   1. Creates a named-pipe server.
//!   2. Writes the pipe name into shared-memory "Local\llcom_smv2_session".
//!   3. Extracts serial_monitor_hook.dll (embedded) to %TEMP%\llcom_smv2\.
//!   4. Injects the hook DLL into the target process via CreateRemoteThread+LoadLibraryW.
//!   5. Runs a worker thread that reads Udata messages from the pipe and
//!      calls the C# callback for each one.
//!
//! UnMonitorComm() signals the worker, waits for it, and ejects the hook DLL.

#![allow(
    non_snake_case,
    clippy::missing_safety_doc,
    unused_imports,
    unused_variables
)]

use std::ffi::c_void;
use std::path::PathBuf;
use std::sync::{Mutex, OnceLock};

use windows::core::PCWSTR;
use windows::Win32::Foundation::{CloseHandle, HANDLE, INVALID_HANDLE_VALUE};
use windows::Win32::Storage::FileSystem::{ReadFile, FILE_FLAG_OVERLAPPED};
use windows::Win32::System::Diagnostics::Debug::WriteProcessMemory;
use windows::Win32::System::Diagnostics::ToolHelp::{
    CreateToolhelp32Snapshot, Module32FirstW, Module32NextW, MODULEENTRY32W,
    TH32CS_SNAPMODULE, TH32CS_SNAPMODULE32,
};
use windows::Win32::System::IO::OVERLAPPED;
use windows::Win32::System::LibraryLoader::{GetModuleHandleW, GetProcAddress};
use windows::Win32::System::Memory::{
    CreateFileMappingW, MapViewOfFile, UnmapViewOfFile, VirtualAllocEx, VirtualFreeEx,
    FILE_MAP_WRITE, MEM_COMMIT, MEM_RELEASE, MEM_RESERVE, PAGE_READWRITE,
};
use windows::Win32::System::Pipes::{
    ConnectNamedPipe, CreateNamedPipeW, PeekNamedPipe,
    PIPE_READMODE_MESSAGE, PIPE_TYPE_MESSAGE, PIPE_WAIT,
};
// PIPE_ACCESS_INBOUND = 0x00000001 (raw constant not exported in this windows version)
const PIPE_ACCESS_INBOUND_FLAG: u32 = 0x00000001;
use windows::Win32::System::Threading::{
    CreateEventW, CreateRemoteThread, GetCurrentProcessId, OpenProcess, SetEvent, Sleep,
    WaitForMultipleObjects, WaitForSingleObject, PROCESS_ALL_ACCESS,
};

use crate::{CallbackFn, Udata, MAX_DATA};

// ── Embedded hook DLL ───────────────────────────────────────────────────────

static HOOK_DLL_BYTES: &[u8] =
    include_bytes!(concat!(env!("OUT_DIR"), "/serial_monitor_hook.dll"));

// ── Send+Sync HANDLE wrapper ────────────────────────────────────────────────

#[derive(Clone, Copy)]
struct SH(*mut c_void);

unsafe impl Send for SH {}
unsafe impl Sync for SH {}

impl SH {
    fn h(self) -> HANDLE {
        HANDLE(self.0)
    }
    fn valid(self) -> bool {
        !self.0.is_null() && self.0 as isize != -1
    }
}

// ── Active monitoring session ───────────────────────────────────────────────

struct Session {
    target_pid: u32,
    pipe: SH,
    shmem: SH,
    stop_event: SH,
    worker: Option<std::thread::JoinHandle<()>>,
}

impl Drop for Session {
    fn drop(&mut self) {
        if self.stop_event.valid() {
            unsafe {
                let _ = SetEvent(self.stop_event.h());
            }
        }
        if let Some(w) = self.worker.take() {
            let _ = w.join();
        }
        for h in [self.pipe, self.shmem, self.stop_event] {
            if h.valid() {
                unsafe {
                    let _ = CloseHandle(h.h());
                }
            }
        }
    }
}

static SESSION: OnceLock<Mutex<Option<Session>>> = OnceLock::new();

fn session_mtx() -> &'static Mutex<Option<Session>> {
    SESSION.get_or_init(|| Mutex::new(None))
}

// ── Hook DLL extraction ─────────────────────────────────────────────────────

fn hook_dll_path() -> PathBuf {
    let mut p = std::env::temp_dir();
    p.push("llcom_smv2");
    let _ = std::fs::create_dir_all(&p);
    p.push("serial_monitor_hook.dll");
    p
}

fn extract_hook_dll(path: &PathBuf) -> bool {
    std::fs::write(path, HOOK_DLL_BYTES).is_ok()
}

// ── Shared memory (hand pipe name to the injected hook DLL) ─────────────────

fn create_shmem(pipe_name: &str) -> Option<SH> {
    let w: Vec<u16> = pipe_name
        .encode_utf16()
        .chain(std::iter::once(0u16))
        .collect();
    let name_w = widestring("Local\\llcom_smv2_session");
    let byte_len = (w.len() * 2).max(512) as u32;

    unsafe {
        let hmap = CreateFileMappingW(
            INVALID_HANDLE_VALUE,
            None,
            PAGE_READWRITE,
            0,
            byte_len,
            PCWSTR(name_w.as_ptr()),
        )
        .ok()?;

        let view = MapViewOfFile(hmap, FILE_MAP_WRITE, 0, 0, byte_len as usize);
        if view.Value.is_null() {
            let _ = CloseHandle(hmap);
            return None;
        }

        let count = w.len().min(256);
        std::ptr::copy_nonoverlapping(w.as_ptr(), view.Value as *mut u16, count);
        let _ = UnmapViewOfFile(view);
        Some(SH(hmap.0))
    }
}

fn widestring(s: &str) -> Vec<u16> {
    s.encode_utf16().chain(std::iter::once(0u16)).collect()
}

// ── Named pipe server ───────────────────────────────────────────────────────

fn create_pipe_server(name: &str) -> Option<SH> {
    let w: Vec<u16> = name
        .encode_utf16()
        .chain(std::iter::once(0u16))
        .collect();

    let pipe = unsafe {
        CreateNamedPipeW(
            PCWSTR(w.as_ptr()),
            windows::Win32::Storage::FileSystem::FILE_FLAGS_AND_ATTRIBUTES(
                PIPE_ACCESS_INBOUND_FLAG | FILE_FLAG_OVERLAPPED.0,
            ),
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
            1,
            0,
            (std::mem::size_of::<Udata>() * 8) as u32,
            0,
            None,
        )
    };

    if pipe == INVALID_HANDLE_VALUE {
        None
    } else {
        Some(SH(pipe.0))
    }
}

// ── DLL injection via CreateRemoteThread + LoadLibraryW ─────────────────────

fn inject_dll(pid: u32, dll_path: &str) -> bool {
    let dll_w: Vec<u16> = dll_path
        .encode_utf16()
        .chain(std::iter::once(0u16))
        .collect();
    let path_bytes = dll_w.len() * 2;

    unsafe {
        let h_proc = match OpenProcess(PROCESS_ALL_ACCESS, false, pid) {
            Ok(h) => h,
            Err(_) => return false,
        };

        let remote =
            VirtualAllocEx(h_proc, None, path_bytes, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if remote.is_null() {
            let _ = CloseHandle(h_proc);
            return false;
        }

        if WriteProcessMemory(h_proc, remote, dll_w.as_ptr() as _, path_bytes, None).is_err() {
            let _ = VirtualFreeEx(h_proc, remote, 0, MEM_RELEASE);
            let _ = CloseHandle(h_proc);
            return false;
        }

        let hk32 = match GetModuleHandleW(windows::core::w!("kernel32.dll")) {
            Ok(h) => h,
            Err(_) => {
                let _ = VirtualFreeEx(h_proc, remote, 0, MEM_RELEASE);
                let _ = CloseHandle(h_proc);
                return false;
            }
        };

        let load_lib = match GetProcAddress(hk32, windows::core::s!("LoadLibraryW")) {
            Some(f) => f,
            None => {
                let _ = VirtualFreeEx(h_proc, remote, 0, MEM_RELEASE);
                let _ = CloseHandle(h_proc);
                return false;
            }
        };

        // SAFETY: LoadLibraryW(LPWSTR) has the same ABI as a remote-thread proc.
        let load_lib_fn: unsafe extern "system" fn(*mut c_void) -> u32 =
            std::mem::transmute(load_lib);

        let result = CreateRemoteThread(
            h_proc,
            None,
            0,
            Some(load_lib_fn),
            Some(remote),
            0,
            None,
        );

        match result {
            Ok(t) => {
                WaitForSingleObject(t, 8_000);
                let _ = CloseHandle(t);
                let _ = VirtualFreeEx(h_proc, remote, 0, MEM_RELEASE);
                let _ = CloseHandle(h_proc);
                true
            }
            Err(_) => {
                let _ = VirtualFreeEx(h_proc, remote, 0, MEM_RELEASE);
                let _ = CloseHandle(h_proc);
                false
            }
        }
    }
}

// ── DLL ejection via CreateRemoteThread + FreeLibrary ───────────────────────

fn eject_dll(pid: u32, dll_filename: &str) {
    let target_upper = dll_filename.to_uppercase();
    unsafe {
        let snap = match CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, pid) {
            Ok(h) => h,
            Err(_) => return,
        };

        let mut me = MODULEENTRY32W {
            dwSize: std::mem::size_of::<MODULEENTRY32W>() as u32,
            ..Default::default()
        };

        let mut module_base: Option<*mut u8> = None;
        if Module32FirstW(snap, &mut me).is_ok() {
            loop {
                let end = me
                    .szModule
                    .iter()
                    .position(|&c| c == 0)
                    .unwrap_or(me.szModule.len());
                let name = String::from_utf16_lossy(&me.szModule[..end]).to_uppercase();
                if name == target_upper {
                    module_base = Some(me.modBaseAddr);
                    break;
                }
                if Module32NextW(snap, &mut me).is_err() {
                    break;
                }
            }
        }
        let _ = CloseHandle(snap);

        let Some(base) = module_base else { return };

        let h_proc = match OpenProcess(PROCESS_ALL_ACCESS, false, pid) {
            Ok(h) => h,
            Err(_) => return,
        };
        let hk32 = match GetModuleHandleW(windows::core::w!("kernel32.dll")) {
            Ok(h) => h,
            Err(_) => {
                let _ = CloseHandle(h_proc);
                return;
            }
        };
        let free_lib = match GetProcAddress(hk32, windows::core::s!("FreeLibrary")) {
            Some(f) => f,
            None => {
                let _ = CloseHandle(h_proc);
                return;
            }
        };

        let free_lib_fn: unsafe extern "system" fn(*mut c_void) -> u32 =
            std::mem::transmute(free_lib);

        if let Ok(t) = CreateRemoteThread(
            h_proc,
            None,
            0,
            Some(free_lib_fn),
            Some(base as *mut c_void),
            0,
            None,
        ) {
            WaitForSingleObject(t, 5_000);
            let _ = CloseHandle(t);
        }
        let _ = CloseHandle(h_proc);
    }
}

// ── Worker thread (reads pipe, dispatches callbacks) ────────────────────────

fn spawn_worker(pipe: SH, stop: SH, cb: CallbackFn) -> std::thread::JoinHandle<()> {
    std::thread::spawn(move || worker_main(pipe, stop, cb))
}

fn worker_main(pipe: SH, stop: SH, callback: CallbackFn) {
    const CONNECT_TIMEOUT_MS: u32 = 10_000;
    const POLL_MS: u32 = 5;
    const UDATA_SIZE: usize = std::mem::size_of::<Udata>();
    const WAIT_OBJ_0: u32 = 0x00000000;

    unsafe {
        // Phase 1: wait for hook DLL to connect
        let conn_ev = match CreateEventW(None, true, false, None) {
            Ok(e) => e,
            Err(_) => return,
        };

        let mut conn_ov: OVERLAPPED = std::mem::zeroed();
        conn_ov.hEvent = conn_ev;

        let conn_result = ConnectNamedPipe(pipe.h(), Some(&mut conn_ov));

        let already_connected = match conn_result {
            Ok(_) => false,
            Err(ref e) => {
                let code = (e.code().0 as u32) & 0xFFFF;
                match code {
                    535 => true,  // ERROR_PIPE_CONNECTED
                    997 => false, // ERROR_IO_PENDING
                    _ => {
                        let _ = CloseHandle(conn_ev);
                        return;
                    }
                }
            }
        };

        if !already_connected {
            let handles = [conn_ev, stop.h()];
            let r = WaitForMultipleObjects(&handles, false, CONNECT_TIMEOUT_MS);
            let _ = CloseHandle(conn_ev);
            if r.0 != WAIT_OBJ_0 {
                return;
            }
        } else {
            let _ = CloseHandle(conn_ev);
        }

        // Phase 2: polling read loop
        loop {
            if WaitForSingleObject(stop.h(), 0).0 == WAIT_OBJ_0 {
                break;
            }

            let mut avail: u32 = 0;
            if PeekNamedPipe(pipe.h(), None, 0, None, Some(&mut avail), None).is_err() {
                break;
            }

            if avail as usize >= UDATA_SIZE {
                let mut ud: Udata = std::mem::zeroed();
                let mut nread: u32 = 0;
                if ReadFile(
                    pipe.h(),
                    Some(std::slice::from_raw_parts_mut(
                        &mut ud as *mut Udata as *mut u8,
                        UDATA_SIZE,
                    )),
                    Some(&mut nread),
                    None,
                )
                .is_ok()
                    && nread as usize >= UDATA_SIZE
                {
                    callback(&ud as *const Udata as *const c_void);
                }
            } else {
                Sleep(POLL_MS);
            }
        }
    }
}

// ── Public platform functions ───────────────────────────────────────────────

pub unsafe fn MonitorComm(pid: u32, _com_index: u32, lp_call_func: CallbackFn) -> i32 {
    UnMonitorComm();

    let our_pid = GetCurrentProcessId();
    let pipe_name = format!("\\\\.\\pipe\\llcom_smv2_{}", our_pid);

    let pipe = match create_pipe_server(&pipe_name) {
        Some(h) => h,
        None => return 0,
    };
    let shmem = match create_shmem(&pipe_name) {
        Some(h) => h,
        None => {
            let _ = CloseHandle(pipe.h());
            return 0;
        }
    };
    let stop_ev = match CreateEventW(None, true, false, None) {
        Ok(e) => SH(e.0),
        Err(_) => {
            let _ = CloseHandle(pipe.h());
            let _ = CloseHandle(shmem.h());
            return 0;
        }
    };

    let worker = spawn_worker(pipe, stop_ev, lp_call_func);

    let hook_path = hook_dll_path();
    let ok = extract_hook_dll(&hook_path)
        && hook_path
            .to_str()
            .map(|s| inject_dll(pid, s))
            .unwrap_or(false);

    if !ok {
        let _ = SetEvent(stop_ev.h());
        let _ = worker.join();
        for h in [pipe, shmem, stop_ev] {
            if h.valid() {
                let _ = CloseHandle(h.h());
            }
        }
        return 0;
    }

    if let Ok(mut g) = session_mtx().lock() {
        *g = Some(Session {
            target_pid: pid,
            pipe,
            shmem,
            stop_event: stop_ev,
            worker: Some(worker),
        });
    }
    1
}

pub unsafe fn UnMonitorComm() -> i32 {
    let pid;
    {
        let Ok(mut g) = session_mtx().lock() else {
            return 1;
        };
        match g.as_ref() {
            Some(s) => pid = s.target_pid,
            None => return 1,
        }
        *g = None;
    }
    eject_dll(pid, "serial_monitor_hook.dll");
    1
}
