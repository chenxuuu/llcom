//! serial_monitor — Cross-platform serial port monitoring library.
//!
//! # Platform support
//!
//! - **Windows**: Full support via DLL injection + kernel32 API hooking.
//! - **Linux/macOS**: Serial port monitoring via process-level hooking is not
//!   currently supported. The exported symbols exist as stubs that return failure
//!   status to maintain API compatibility.
//!
//! # Exported functions (C ABI)
//!
//! ```c
//! BOOL MonitorComm(UINT Pid, UINT ComIndex, CallbackFn lpCallFunc);
//! BOOL UnMonitorComm(void);
//! const char* SerialMonitorGetVersion(void);
//! ```
//!
//! # Udata wire format (Pack=1, must match C# struct)
//!
//! byte  com_port     COM port number
//! byte  comm_state   2=Disconnect  3=Receive  4=Send
//! i32   file_handle  Windows HANDLE (truncated for compatibility)
//! i32   data_size    valid bytes in data[]
//! [u8; 8192]  data   payload

#![allow(non_snake_case)]

// ── Common types ────────────────────────────────────────────────────────────

use std::ffi::c_void;

const MAX_DATA: usize = 8192;

#[repr(C, packed(1))]
#[derive(Clone, Copy)]
#[allow(dead_code)]
struct Udata {
    com_port: u8,
    comm_state: u8,
    file_handle: i32,
    data_size: i32,
    data: [u8; MAX_DATA],
}

/// Callback type passed by C# via [DllImport].
/// `delegate int CallbackDelegate(IntPtr param)` → stdcall/extern fn ptr.
pub type CallbackFn = unsafe extern "system" fn(*const c_void) -> i32;

// ── Platform-specific implementation ────────────────────────────────────────

#[cfg(windows)]
#[path = "platform/windows.rs"]
mod platform;

#[cfg(unix)]
#[path = "platform/unix.rs"]
mod platform;

// ── Exported API ────────────────────────────────────────────────────────────

/// Inject hook DLL into `pid` and start monitoring.
/// Returns BOOL (1 = success, 0 = failure).
///
/// On non-Windows platforms, this always returns 0.
#[no_mangle]
pub unsafe extern "system" fn MonitorComm(
    pid: u32,
    com_index: u32,
    lp_call_func: CallbackFn,
) -> i32 {
    platform::MonitorComm(pid, com_index, lp_call_func)
}

/// Stop monitoring. Always returns 1.
#[no_mangle]
pub unsafe extern "system" fn UnMonitorComm() -> i32 {
    platform::UnMonitorComm()
}

/// Get version string. Returns a static C string.
#[no_mangle]
pub unsafe extern "system" fn SerialMonitorGetVersion() -> *const u8 {
    b"serial_monitor_rs v0.1.0\0".as_ptr()
}

// ── DllMain / shared-library init ──────────────────────────────────────────

#[cfg(windows)]
#[no_mangle]
pub unsafe extern "system" fn DllMain(
    _hinst: *mut c_void,
    _reason: u32,
    _res: *mut c_void,
) -> i32 {
    1
}
