@echo off
REM Build script for Serial Monitor DLL
REM Builds both x86 and x64 versions and copies them to the llcom project

echo Building Serial Monitor DLL...
echo.

echo [1/4] Building x86 (32-bit) version...
cargo build --release --target i686-pc-windows-msvc
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: x86 build failed!
    exit /b 1
)
echo OK

echo.
echo [2/4] Building x64 (64-bit) version...
cargo build --release --target x86_64-pc-windows-msvc
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: x64 build failed!
    exit /b 1
)
echo OK

echo.
echo [3/4] Copying x86 DLL to costura32...
copy /Y target\i686-pc-windows-msvc\release\serial_monitor.dll ..\llcom\costura32\serial_monitor.dll
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to copy x86 DLL!
    exit /b 1
)
echo OK

echo.
echo [4/4] Copying x64 DLL to costura64...
copy /Y target\x86_64-pc-windows-msvc\release\serial_monitor.dll ..\llcom\costura64\serial_monitor.dll
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to copy x64 DLL!
    exit /b 1
)
echo OK

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo DLLs have been copied to:
echo   - ../llcom/costura32/serial_monitor.dll
echo   - ../llcom/costura64/serial_monitor.dll
echo.
echo You can now rebuild the llcom project.
