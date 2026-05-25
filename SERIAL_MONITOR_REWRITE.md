# Serial Monitor DLL Rewrite - Rust Implementation

This directory contains a complete rewrite of the serial port monitoring functionality for llcom. The closed-source Delphi DLL has been replaced with an open-source Rust implementation.

## Quick Start

### For Users
The new DLLs will be automatically built by GitHub Actions and included in releases. No action required.

### For Developers (Windows)

1. **Install Prerequisites**
   ```bash
   # Install Rust from https://rustup.rs/

   # Add Windows targets
   rustup target add i686-pc-windows-msvc x86_64-pc-windows-msvc
   ```

2. **Build the DLLs**
   ```bash
   cd serial_monitor_rust
   build.bat
   ```

   This will build both x86 and x64 versions and copy them to the appropriate locations.

3. **Rebuild llcom**
   Build the llcom C# project normally. The new DLLs will be embedded.

## What's New

### ✅ Full x64 Support
The new implementation works on both 32-bit and 64-bit versions of Windows and llcom.

### ✅ Improved Stability
Uses battle-tested hooking mechanisms that are less likely to cause crashes in monitored applications.

### ✅ Open Source
All code is available for review, modification, and improvement.

### ✅ Better Compatibility
Should work with more serial port applications that previously crashed or didn't work.

## Project Structure

```
serial_monitor_rust/
├── src/
│   └── lib.rs              # Main implementation
├── Cargo.toml              # Rust project configuration
├── build.bat               # Windows build script
├── README.md               # Technical documentation
├── MIGRATION.md            # Migration guide
└── .gitignore             # Git ignore rules

.github/workflows/
└── build-serial-monitor.yml # CI/CD configuration
```

## Documentation

- **[README.md](serial_monitor_rust/README.md)** - Technical documentation and API reference
- **[MIGRATION.md](serial_monitor_rust/MIGRATION.md)** - Detailed migration guide and testing checklist
- **[Cargo.toml](serial_monitor_rust/Cargo.toml)** - Project dependencies and configuration

## Technical Overview

The implementation uses:
- **Rust** for memory safety and modern tooling
- **retour** crate for safe API hooking
- **windows** crate for Windows API bindings
- **parking_lot** for efficient synchronization

### How It Works

1. **DLL Injection**: Injects the monitoring DLL into the target process using `CreateRemoteThread`
2. **API Hooking**: Hooks Windows API functions (`ReadFile`, `WriteFile`, `CreateFileW`)
3. **Data Capture**: Intercepts serial port operations and captures the data
4. **Callback**: Calls the C# callback function with captured data

### Architecture Diagram

```
┌─────────────┐
│   llcom     │
│  (C# WPF)   │
└──────┬──────┘
       │ P/Invoke
       ▼
┌──────────────────────┐
│ serial_monitor.dll   │
│  (Rust, this project)│
└──────┬───────────────┘
       │ DLL Injection
       ▼
┌─────────────────────────┐
│ Target Serial App       │
│ (XCOM, JCOM, etc.)      │
│  ┌──────────────────┐   │
│  │ Hooked Functions │   │
│  │  - ReadFile      │   │
│  │  - WriteFile     │   │
│  │  - CreateFileW   │   │
│  └──────────────────┘   │
└─────────────────────────┘
```

## Building from Source

### Automated Build (Recommended)
Push changes to `serial_monitor_rust/` and GitHub Actions will automatically build both versions.

### Manual Build

**On Windows:**
```bash
cd serial_monitor_rust
cargo build --release --target i686-pc-windows-msvc
cargo build --release --target x86_64-pc-windows-msvc
```

**Cross-compile from Linux (Advanced):**
Requires xwin setup. See [rust-cross documentation](https://rust-lang.github.io/rustup-components-history/) for details.

## Testing

Before releasing, test with various serial port applications:
- SSCOM (reference, should work)
- XCOM (previously crashed)
- LLCOM itself (previously crashed)
- JCOM (previously didn't work)
- Other tools mentioned in issue #XXX

See [MIGRATION.md](serial_monitor_rust/MIGRATION.md) for detailed testing checklist.

## Troubleshooting

### Build Issues
- Ensure Rust is installed: `rustc --version`
- Add Windows targets: `rustup target add i686-pc-windows-msvc x86_64-pc-windows-msvc`
- Install Visual Studio with C++ build tools

### Runtime Issues
- Check architecture matches (x86 app needs x86 DLL)
- Run llcom as Administrator if injection fails
- Check Windows Event Viewer for detailed error messages

## Contributing

Contributions welcome! Please:
1. Follow Rust coding conventions
2. Test on both x86 and x64
3. Update documentation
4. Add comments for complex logic

## License

Same as parent llcom project.

## Credits

- Original concept: llcom project
- Rust implementation: Written from scratch based on API documentation
- Special thanks to: Rust community, retour crate authors

## Status

- [x] Core implementation complete
- [x] Build infrastructure setup
- [x] Documentation written
- [ ] Comprehensive testing
- [ ] Release ready

## Links

- [Issue Tracker](https://github.com/chenxuuu/llcom/issues)
- [Rust Documentation](https://doc.rust-lang.org/)
- [Windows API Reference](https://docs.microsoft.com/en-us/windows/win32/)
