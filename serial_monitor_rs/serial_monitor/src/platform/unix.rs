//! Unix (Linux/macOS) stub implementation for serial port monitoring.
//!
//! Process-level serial port monitoring via DLL injection is not supported on
//! Unix platforms. These functions exist as no-op stubs to maintain ABI
//! compatibility, allowing the calling application to gracefully handle
//! unsupported platforms.

#![allow(non_snake_case, unused_variables)]

use crate::CallbackFn;

/// Always returns 0 (failure) on Unix platforms.
///
/// Serial port monitoring via process injection is not supported on Linux/macOS.
/// Consider using alternative approaches like `socat` relay or `strace` for
/// debugging serial port traffic.
pub unsafe fn MonitorComm(pid: u32, _com_index: u32, _lp_call_func: CallbackFn) -> i32 {
    // Serial monitor via process injection is not supported on this platform.
    let _ = pid;
    0
}

/// Always returns 1 (no-op success) on Unix platforms.
pub unsafe fn UnMonitorComm() -> i32 {
    1
}
