# Serial Monitor Rust - README

## Overview

This is a Rust rewrite of the serial port monitoring DLL for llcom. It provides functionality to monitor serial port communications in other Windows processes by injecting a DLL and hooking Windows API functions.

## Features

- ✅ Monitor serial port read/write operations in any Windows process
- ✅ Compatible with both x86 and x64 architectures
- ✅ Safe API hooking using the `retour` crate
- ✅ Drop-in replacement for the original `serial_monitor.dll`
- ✅ Open source and maintainable

## Building

### Prerequisites

1. Install Rust from https://rustup.rs/
2. Add Windows targets:
   ```bash
   rustup target add i686-pc-windows-msvc
   rustup target add x86_64-pc-windows-msvc
   ```
3. Install Visual Studio with C++ build tools (required for MSVC toolchain)

### Build Commands

Build for x86 (32-bit):
```bash
cargo build --release --target i686-pc-windows-msvc
```

Build for x64 (64-bit):
```bash
cargo build --release --target x86_64-pc-windows-msvc
```

The compiled DLLs will be in:
- `target/i686-pc-windows-msvc/release/serial_monitor.dll`
- `target/x86_64-pc-windows-msvc/release/serial_monitor.dll`

## Installation

1. Build both x86 and x64 versions
2. Copy the DLLs to the appropriate locations in the llcom project:
   - Copy `target/i686-pc-windows-msvc/release/serial_monitor.dll` to `llcom/costura32/`
   - Copy `target/x86_64-pc-windows-msvc/release/serial_monitor.dll` to `llcom/costura64/`
3. Rebuild the llcom project

## API

The DLL exports two functions that are compatible with the original interface:

### MonitorComm
```c
bool MonitorComm(uint32_t pid, uint32_t com_index, CallbackFn callback);
```

Starts monitoring a COM port in the specified process.

**Parameters:**
- `pid`: Process ID of the target application
- `com_index`: COM port number (e.g., 1 for COM1)
- `callback`: Callback function pointer

**Returns:** `true` on success, `false` on failure

### UnMonitorComm
```c
bool UnMonitorComm();
```

Stops monitoring and removes hooks.

**Returns:** `true` on success, `false` on failure

### Callback Data Structure
```c
#[repr(C, packed(1))]
struct Udata {
    com_port: u8,        // COM port number
    comm_state: u8,      // State: 2=Disconnect, 3=Receive, 4=Send
    file_handle: i32,    // File handle
    data_size: i32,      // Size of data
    data: [u8; 8192],    // Data buffer
}
```

## Implementation Details

### Hooking Mechanism

The implementation uses the `retour` crate which provides safe Rust bindings for function detouring. It hooks three kernel32.dll functions:

1. **ReadFile** - Intercepts data reads from serial port
2. **WriteFile** - Intercepts data writes to serial port
3. **CreateFileW** - Tracks COM port file handles

### Process Injection

The DLL is injected into the target process using:
1. `OpenProcess` to get a handle to the target process
2. `VirtualAllocEx` to allocate memory in the target process
3. `WriteProcessMemory` to write the DLL path
4. `CreateRemoteThread` with `LoadLibraryW` to load the DLL

### Thread Safety

All global state is protected using:
- `parking_lot::Mutex` for thread-safe access
- `AtomicPtr` for lock-free callback storage
- `once_cell::Lazy` for lazy static initialization

## Advantages Over Original DLL

1. **Open Source**: Fully transparent and auditable code
2. **Maintainable**: Written in modern Rust with clear documentation
3. **x64 Support**: Native support for both x86 and x64 architectures
4. **Memory Safe**: Rust's safety guarantees reduce potential bugs
5. **Modern Tooling**: Easy to build and modify with cargo

## Known Limitations

1. Hooks remain in the target process even after `UnMonitorComm` (requires process restart to fully clean up)
2. Requires Administrator privileges to inject into some processes
3. May not work with processes that have anti-debugging/anti-hooking protections

## Troubleshooting

### Build fails with "can't find crate for `core`"
- Make sure you've added the Windows targets: `rustup target add i686-pc-windows-msvc x86_64-pc-windows-msvc`
- Ensure you're building on Windows or have proper cross-compilation setup

### DLL injection fails
- Target process may require Administrator privileges
- Target process may have anti-hooking protections
- Ensure you're using the correct architecture (x86 DLL for x86 process, x64 for x64)

### Monitoring doesn't capture data
- Verify the COM port number is correct
- Check that the target application is actually using the serial port
- Ensure callback function is properly defined

## License

This project follows the same license as the parent llcom project.

## Contributing

Contributions are welcome! Please ensure:
1. Code follows Rust best practices
2. All changes are tested on both x86 and x64
3. Documentation is updated accordingly
