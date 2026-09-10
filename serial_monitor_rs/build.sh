#!/usr/bin/env bash
# Build script for serial_monitor Rust native libraries
# Run from the serial_monitor_rs directory
#
# Usage:
#   ./build.sh                  # x86_64 Release (default)
#   ./build.sh --debug          # x86_64 Debug
#   ./build.sh --target aarch64-unknown-linux-gnu  # cross-compile
#
# Outputs:
#   Linux  →  ../llcom/native/linux-x64/libserial_monitor.so
#   macOS  →  ../llcom/native/osx-x64/libserial_monitor.dylib
#   Windows → ../llcom/costura64/serial_monitor.dll

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIG="release"
PROFILE="release"
TARGET=""
OUTPUT_DIR=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --debug)
            CONFIG="debug"
            PROFILE="debug"
            shift
            ;;
        --target)
            TARGET="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Auto-detect target
if [ -z "$TARGET" ]; then
    ARCH=$(uname -m)
    OS=$(uname -s)
    case "$OS" in
        Linux)
            case "$ARCH" in
                x86_64|amd64) TARGET="x86_64-unknown-linux-gnu" ;;
                aarch64|arm64) TARGET="aarch64-unknown-linux-gnu" ;;
                armv7l) TARGET="armv7-unknown-linux-gnueabihf" ;;
                *) echo "Unsupported architecture: $ARCH"; exit 1 ;;
            esac
            ;;
        Darwin)
            case "$ARCH" in
                x86_64|amd64) TARGET="x86_64-apple-darwin" ;;
                arm64) TARGET="aarch64-apple-darwin" ;;
                *) echo "Unsupported architecture: $ARCH"; exit 1 ;;
            esac
            ;;
        MINGW*|MSYS*|CYGWIN*)
            case "$ARCH" in
                x86_64|amd64) TARGET="x86_64-pc-windows-msvc" ;;
                *) echo "Unsupported architecture: $ARCH"; exit 1 ;;
            esac
            ;;
        *) echo "Unsupported OS: $OS"; exit 1 ;;
    esac
fi

echo ""
echo "=== Building serial_monitor for $TARGET ($CONFIG) ==="

# Check for Rust
if ! command -v cargo &> /dev/null; then
    echo "Error: cargo not found. Please install Rust: https://rustup.rs"
    exit 1
fi

# Add target if needed
HAS_TARGET=$(rustup target list --installed | grep -c "^$TARGET$" || true)
if [ "$HAS_TARGET" -eq 0 ]; then
    echo "Installing target: $TARGET"
    rustup target add "$TARGET"
fi

# Build
CARGO_ARGS=""
if [ "$CONFIG" = "release" ]; then
    CARGO_ARGS="--release"
fi

echo "Building serial_monitor..."
cargo build $CARGO_ARGS -p serial_monitor --target "$TARGET"

# Determine library extension
LIB_EXT=".so"
if [[ "$TARGET" == *apple* ]]; then
    LIB_EXT=".dylib"
elif [[ "$TARGET" == *windows* ]]; then
    LIB_EXT=".dll"
fi

# Determine output subdirectory
case "$TARGET" in
    *linux*)    OUTPUT_DIR="linux-x64" ;;
    *apple*)    OUTPUT_DIR="osx-x64" ;;
    *windows*)  OUTPUT_DIR="win-x64" ;;
    *)          OUTPUT_DIR="unknown" ;;
esac

LIB_SRC="$SCRIPT_DIR/target/$TARGET/$PROFILE/libserial_monitor$LIB_EXT"
if [ ! -f "$LIB_SRC" ]; then
    # Try with OS prefix
    LIB_SRC="$SCRIPT_DIR/target/$TARGET/$PROFILE/serial_monitor$LIB_EXT"
fi

if [ ! -f "$LIB_SRC" ]; then
    echo "Error: Built library not found. Expected: $LIB_SRC"
    echo "Checking target directory..."
    find "$SCRIPT_DIR/target/$TARGET/$PROFILE" -name "*serial_monitor*" 2>/dev/null || true
    exit 1
fi

# Copy to output
DST_DIR="$SCRIPT_DIR/../llcom/native/$OUTPUT_DIR"
mkdir -p "$DST_DIR"
cp -f "$LIB_SRC" "$DST_DIR/$(basename "$LIB_SRC")"
echo "  Copied → $DST_DIR/$(basename "$LIB_SRC") ($(du -h "$LIB_SRC" | cut -f1))"

echo ""
echo "Build complete: $TARGET"
