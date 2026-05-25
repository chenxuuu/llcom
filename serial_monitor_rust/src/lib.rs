//! Serial Port Monitoring Library for Windows
//!
//! This library provides functionality to monitor serial port communications
//! in other processes by injecting a DLL and hooking Windows API functions.
//!
//! Compatible with both x86 and x64 architectures.

#![allow(non_snake_case)]
#![allow(non_camel_case_types)]

use once_cell::sync::Lazy;
use parking_lot::Mutex;
use retour::static_detour;
use std::ffi::c_void;
use std::ptr;
use std::sync::atomic::{AtomicPtr, Ordering};
use windows::core::*;
use windows::Win32::Foundation::*;
use windows::Win32::Storage::FileSystem::*;
use windows::Win32::System::LibraryLoader::*;
use windows::Win32::System::Memory::*;
use windows::Win32::System::Threading::*;

/// Data structure passed to the callback function
#[repr(C, packed(1))]
pub struct Udata {
    pub com_port: u8,
    pub comm_state: u8,
    pub file_handle: i32,
    pub data_size: i32,
    pub data: [u8; 8192],
}

/// Communication state constants
const STATE_DISCONNECT: u8 = 2;
const STATE_RECEIVE: u8 = 3;
const STATE_SEND: u8 = 4;

/// Callback function type
type CallbackFn = unsafe extern "C" fn(*const Udata) -> i32;

/// Global state for the monitoring system
struct MonitorState {
    callback: Option<CallbackFn>,
    target_pid: u32,
    target_com_port: u32,
    is_monitoring: bool,
}

static MONITOR_STATE: Lazy<Mutex<MonitorState>> = Lazy::new(|| {
    Mutex::new(MonitorState {
        callback: None,
        target_pid: 0,
        target_com_port: 0,
        is_monitoring: false,
    })
});

// Shared memory for inter-process communication
static SHARED_CALLBACK: AtomicPtr<c_void> = AtomicPtr::new(ptr::null_mut());
static SHARED_COM_PORT: Mutex<u32> = Mutex::new(0);

// Track COM port file handles
static COM_HANDLES: Lazy<Mutex<Vec<(HANDLE, u32)>>> = Lazy::new(|| Mutex::new(Vec::new()));

// Define detours for the API functions
static_detour! {
    static ReadFileDetour: unsafe extern "system" fn(HANDLE, *mut c_void, u32, *mut u32, *mut OVERLAPPED) -> BOOL;
    static WriteFileDetour: unsafe extern "system" fn(HANDLE, *const c_void, u32, *mut u32, *mut OVERLAPPED) -> BOOL;
    static CreateFileWDetour: unsafe extern "system" fn(PCWSTR, FILE_ACCESS_FLAGS, FILE_SHARE_MODE, *const SECURITY_ATTRIBUTES, FILE_CREATION_DISPOSITION, FILE_FLAGS_AND_ATTRIBUTES, HANDLE) -> HANDLE;
}

/// Hook for ReadFile
unsafe extern "system" fn read_file_hook(
    hFile: HANDLE,
    lpBuffer: *mut c_void,
    nNumberOfBytesToRead: u32,
    lpNumberOfBytesRead: *mut u32,
    lpOverlapped: *mut OVERLAPPED,
) -> BOOL {
    // Call the original function
    let result = ReadFileDetour.call(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);

    // Check if this is a COM port handle we're monitoring
    if result.as_bool() && !lpBuffer.is_null() && !lpNumberOfBytesRead.is_null() {
        let bytes_read = *lpNumberOfBytesRead;
        if bytes_read > 0 && bytes_read <= 8192 {
            let handles = COM_HANDLES.lock();
            if let Some((_, com_port)) = handles.iter().find(|(h, _)| *h == hFile) {
                // Call the callback with received data
                invoke_callback(STATE_RECEIVE, *com_port, hFile, lpBuffer as *const u8, bytes_read as usize);
            }
        }
    }

    result
}

/// Hook for WriteFile
unsafe extern "system" fn write_file_hook(
    hFile: HANDLE,
    lpBuffer: *const c_void,
    nNumberOfBytesToWrite: u32,
    lpNumberOfBytesWritten: *mut u32,
    lpOverlapped: *mut OVERLAPPED,
) -> BOOL {
    // Check if this is a COM port handle we're monitoring before the write
    let mut should_monitor = false;
    let mut com_port = 0;

    if !lpBuffer.is_null() && nNumberOfBytesToWrite > 0 && nNumberOfBytesToWrite <= 8192 {
        let handles = COM_HANDLES.lock();
        if let Some((_, port)) = handles.iter().find(|(h, _)| *h == hFile) {
            should_monitor = true;
            com_port = *port;
        }
    }

    // Call the original function
    let result = WriteFileDetour.call(hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, lpOverlapped);

    // Call callback with sent data
    if should_monitor && result.as_bool() && !lpNumberOfBytesWritten.is_null() {
        let bytes_written = *lpNumberOfBytesWritten;
        if bytes_written > 0 {
            invoke_callback(STATE_SEND, com_port, hFile, lpBuffer as *const u8, bytes_written as usize);
        }
    }

    result
}

/// Hook for CreateFileW to track COM port handles
unsafe extern "system" fn create_file_w_hook(
    lpFileName: PCWSTR,
    dwDesiredAccess: FILE_ACCESS_FLAGS,
    dwShareMode: FILE_SHARE_MODE,
    lpSecurityAttributes: *const SECURITY_ATTRIBUTES,
    dwCreationDisposition: FILE_CREATION_DISPOSITION,
    dwFlagsAndAttributes: FILE_FLAGS_AND_ATTRIBUTES,
    hTemplateFile: HANDLE,
) -> HANDLE {
    // Call the original function
    let result = CreateFileWDetour.call(
        lpFileName,
        dwDesiredAccess,
        dwShareMode,
        lpSecurityAttributes,
        dwCreationDisposition,
        dwFlagsAndAttributes,
        hTemplateFile,
    );

    // Check if this is a COM port
    if result != INVALID_HANDLE_VALUE && !lpFileName.is_null() {
        let filename = lpFileName.to_string();
        if let Ok(name) = filename {
            let name_upper = name.to_uppercase();
            // Check for COM port patterns: COM1, COM2, \\.\COM1, etc.
            if name_upper.contains("COM") {
                if let Some(com_str) = extract_com_number(&name_upper) {
                    if let Ok(com_num) = com_str.parse::<u32>() {
                        let target_com = *SHARED_COM_PORT.lock();
                        if com_num == target_com {
                            let mut handles = COM_HANDLES.lock();
                            handles.push((result, com_num));
                        }
                    }
                }
            }
        }
    }

    result
}

/// Extract COM port number from filename
fn extract_com_number(name: &str) -> Option<String> {
    if let Some(pos) = name.find("COM") {
        let after_com = &name[pos + 3..];
        let num_str: String = after_com.chars().take_while(|c| c.is_numeric()).collect();
        if !num_str.is_empty() {
            return Some(num_str);
        }
    }
    None
}

/// Invoke the callback function with captured data
unsafe fn invoke_callback(state: u8, com_port: u32, handle: HANDLE, data: *const u8, size: usize) {
    let callback_ptr = SHARED_CALLBACK.load(Ordering::SeqCst);
    if callback_ptr.is_null() {
        return;
    }

    let callback: CallbackFn = std::mem::transmute(callback_ptr);

    let mut udata = Udata {
        com_port: com_port as u8,
        comm_state: state,
        file_handle: handle.0 as i32,
        data_size: size.min(8192) as i32,
        data: [0u8; 8192],
    };

    // Copy data
    if !data.is_null() && size > 0 {
        let copy_size = size.min(8192);
        std::ptr::copy_nonoverlapping(data, udata.data.as_mut_ptr(), copy_size);
    }

    callback(&udata);
}

/// Install hooks in the target process
unsafe fn install_hooks() -> Result<()> {
    let kernel32 = GetModuleHandleW(w!("kernel32.dll"))?;

    // Get addresses of functions to hook
    let read_file_addr = GetProcAddress(kernel32, s!("ReadFile"))
        .ok_or_else(|| Error::from(E_FAIL))?;
    let write_file_addr = GetProcAddress(kernel32, s!("WriteFile"))
        .ok_or_else(|| Error::from(E_FAIL))?;
    let create_file_w_addr = GetProcAddress(kernel32, s!("CreateFileW"))
        .ok_or_else(|| Error::from(E_FAIL))?;

    // Initialize detours
    ReadFileDetour
        .initialize(std::mem::transmute(read_file_addr), read_file_hook)
        .map_err(|_| Error::from(E_FAIL))?
        .enable()
        .map_err(|_| Error::from(E_FAIL))?;

    WriteFileDetour
        .initialize(std::mem::transmute(write_file_addr), write_file_hook)
        .map_err(|_| Error::from(E_FAIL))?
        .enable()
        .map_err(|_| Error::from(E_FAIL))?;

    CreateFileWDetour
        .initialize(std::mem::transmute(create_file_w_addr), create_file_w_hook)
        .map_err(|_| Error::from(E_FAIL))?
        .enable()
        .map_err(|_| Error::from(E_FAIL))?;

    Ok(())
}

/// Remove hooks
unsafe fn remove_hooks() {
    let _ = ReadFileDetour.disable();
    let _ = WriteFileDetour.disable();
    let _ = CreateFileWDetour.disable();

    COM_HANDLES.lock().clear();
}

/// DLL entry point
#[no_mangle]
#[allow(non_snake_case)]
unsafe extern "system" fn DllMain(
    _hinst_dll: HINSTANCE,
    fdw_reason: u32,
    _lpv_reserved: *mut c_void,
) -> BOOL {
    match fdw_reason {
        DLL_PROCESS_ATTACH => {
            // When injected into target process, install hooks
            if let Ok(()) = install_hooks() {
                TRUE
            } else {
                FALSE
            }
        }
        DLL_PROCESS_DETACH => {
            // Clean up hooks
            remove_hooks();
            TRUE
        }
        _ => TRUE,
    }
}

/// Start monitoring a COM port in the specified process
///
/// # Safety
/// This function performs process injection and API hooking
#[no_mangle]
pub unsafe extern "C" fn MonitorComm(pid: u32, com_index: u32, callback: CallbackFn) -> bool {
    let mut state = MONITOR_STATE.lock();

    if state.is_monitoring {
        return false;
    }

    // Store callback and COM port info
    SHARED_CALLBACK.store(callback as *mut c_void, Ordering::SeqCst);
    *SHARED_COM_PORT.lock() = com_index;

    // Open target process
    let process_handle = match OpenProcess(
        PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ | PROCESS_QUERY_INFORMATION,
        false,
        pid,
    ) {
        Ok(handle) => handle,
        Err(_) => return false,
    };

    // Get path to this DLL
    let mut dll_path = [0u16; 512];
    let mut hmodule = HMODULE::default();
    if !GetModuleHandleExW(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        PCWSTR(MonitorComm as *const u16),
        &mut hmodule,
    ).as_bool() {
        let _ = CloseHandle(process_handle);
        return false;
    }

    let dll_path_len = GetModuleFileNameW(hmodule, &mut dll_path);
    if dll_path_len == 0 {
        let _ = CloseHandle(process_handle);
        return false;
    }

    // Allocate memory in target process for DLL path
    let dll_path_size = ((dll_path_len as usize + 1) * 2) as usize;
    let remote_dll_path = match VirtualAllocEx(
        process_handle,
        None,
        dll_path_size,
        MEM_COMMIT | MEM_RESERVE,
        PAGE_READWRITE,
    ) {
        ptr if !ptr.is_null() => ptr,
        _ => {
            let _ = CloseHandle(process_handle);
            return false;
        }
    };

    // Write DLL path to target process
    if !WriteProcessMemory(
        process_handle,
        remote_dll_path,
        dll_path.as_ptr() as *const c_void,
        dll_path_size,
        None,
    ).as_bool() {
        VirtualFreeEx(process_handle, remote_dll_path, 0, MEM_RELEASE);
        let _ = CloseHandle(process_handle);
        return false;
    }

    // Get LoadLibraryW address (same in all processes)
    let kernel32 = match GetModuleHandleW(w!("kernel32.dll")) {
        Ok(h) => h,
        Err(_) => {
            VirtualFreeEx(process_handle, remote_dll_path, 0, MEM_RELEASE);
            let _ = CloseHandle(process_handle);
            return false;
        }
    };

    let load_library_addr = match GetProcAddress(kernel32, s!("LoadLibraryW")) {
        Some(addr) => addr,
        None => {
            VirtualFreeEx(process_handle, remote_dll_path, 0, MEM_RELEASE);
            let _ = CloseHandle(process_handle);
            return false;
        }
    };

    // Create remote thread to load our DLL
    let remote_thread = match CreateRemoteThread(
        process_handle,
        None,
        0,
        Some(std::mem::transmute(load_library_addr)),
        Some(remote_dll_path),
        0,
        None,
    ) {
        Ok(thread) => thread,
        Err(_) => {
            VirtualFreeEx(process_handle, remote_dll_path, 0, MEM_RELEASE);
            let _ = CloseHandle(process_handle);
            return false;
        }
    };

    // Wait for injection to complete
    let _ = WaitForSingleObject(remote_thread, 5000);

    // Clean up
    let _ = CloseHandle(remote_thread);
    VirtualFreeEx(process_handle, remote_dll_path, 0, MEM_RELEASE);
    let _ = CloseHandle(process_handle);

    state.callback = Some(callback);
    state.target_pid = pid;
    state.target_com_port = com_index;
    state.is_monitoring = true;

    true
}

/// Stop monitoring
#[no_mangle]
pub unsafe extern "C" fn UnMonitorComm() -> bool {
    let mut state = MONITOR_STATE.lock();

    if !state.is_monitoring {
        return true;
    }

    // Clear callback
    SHARED_CALLBACK.store(ptr::null_mut(), Ordering::SeqCst);

    // Note: We don't forcefully unload from the target process
    // as that could cause crashes. The hooks will remain but won't
    // callback anymore. A process restart will clean everything up.

    state.callback = None;
    state.is_monitoring = false;
    state.target_pid = 0;
    state.target_com_port = 0;

    true
}
