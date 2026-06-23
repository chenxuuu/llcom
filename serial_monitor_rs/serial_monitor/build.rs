//! Build script for serial_monitor shared library.
//!
//! On Windows: embeds serial_monitor_hook.dll for injection.
//! On Linux/macOS: no embedded DLL needed (stub implementation).

use std::path::PathBuf;

fn main() {
    let target_os = std::env::var("CARGO_CFG_TARGET_OS").unwrap_or_default();

    if target_os == "windows" {
        embed_hook_dll();
    } else {
        // Unix: no embedded hook DLL needed
        println!("cargo:warning=Building serial_monitor for non-Windows target (stub mode)");
    }
}

fn embed_hook_dll() {
    let manifest_dir =
        PathBuf::from(std::env::var("CARGO_MANIFEST_DIR").expect("CARGO_MANIFEST_DIR not set"));
    let workspace_root = manifest_dir
        .parent()
        .expect("serial_monitor crate has no parent directory");

    let target = std::env::var("TARGET").unwrap_or_else(|_| "x86_64-pc-windows-msvc".into());
    let profile = std::env::var("PROFILE").unwrap_or_else(|_| "release".into());

    let hook_dll_src = workspace_root
        .join("target")
        .join(&target)
        .join(&profile)
        .join("serial_monitor_hook.dll");

    if !hook_dll_src.exists() {
        panic!(
            "\n\nserial_monitor_hook.dll not found.\nExpected: {}\n\n\
             Build the hook DLL first, then re-build serial_monitor:\n\
             \n  cargo build{} -p serial_monitor_hook --target {}\n\
             \nOr just run: .\\build.ps1\n",
            hook_dll_src.display(),
            if profile == "release" { " --release" } else { "" },
            target
        );
    }

    let out_dir = PathBuf::from(std::env::var("OUT_DIR").expect("OUT_DIR not set"));
    let dst = out_dir.join("serial_monitor_hook.dll");
    std::fs::copy(&hook_dll_src, &dst)
        .expect("Failed to copy serial_monitor_hook.dll to OUT_DIR");

    println!("cargo:rerun-if-changed={}", hook_dll_src.display());
    println!(
        "cargo:rerun-if-changed={}",
        workspace_root
            .join("serial_monitor_hook")
            .join("src")
            .join("lib.rs")
            .display()
    );
}
