using System.Runtime.InteropServices;

namespace llcom.Tools;

/// <summary>
/// Cross-platform native interop for serial_monitor library.
/// On Windows: loads serial_monitor.dll from embedded resources.
/// On Linux: loads libserial_monitor.so from native/linux-x64/.
/// On macOS: loads libserial_monitor.dylib from native/osx-x64/.
/// </summary>
public static class NativeInterop
{
    /// <summary>Callback delegate for MonitorComm (matches C ABI).</summary>
    public delegate int MonitorCallback(IntPtr param);

    // ── Struct must match C Udata (Pack=1) ─────────────────────────────

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Udata
    {
        public byte ComPort;
        public byte CommState;
        public int FileHandle;
        public int DataSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8192)]
        public byte[] Data;
    }

    // ── State enum ─────────────────────────────────────────────────────

    public enum CommState : byte
    {
        Disconnect = 2,
        Receive = 3,
        Send = 4
    }

    // ── Native delegates ───────────────────────────────────────────────

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool MonitorCommDelegate(uint pid, uint comIndex, MonitorCallback callback);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool UnMonitorCommDelegate();

    private static IntPtr _nativeLib = IntPtr.Zero;
    private static MonitorCommDelegate? _monitorComm;
    private static UnMonitorCommDelegate? _unMonitorComm;

    /// <summary>Whether the native serial_monitor library is available.</summary>
    public static bool IsAvailable => _nativeLib != IntPtr.Zero;

    /// <summary>Platform-specific error message if not available.</summary>
    public static string AvailabilityMessage { get; private set; } = "";

    static NativeInterop()
    {
        try
        {
            LoadNativeLibrary();
        }
        catch (Exception ex)
        {
            AvailabilityMessage = $"Failed to load serial_monitor: {ex.Message}";
        }
    }

    private static void LoadNativeLibrary()
    {
        if (PlatformHelper.IsWindows)
        {
            // Windows: try to load serial_monitor.dll from app directory
            var dllPath = Path.Combine(PlatformHelper.AppPath, "serial_monitor.dll");
            if (!File.Exists(dllPath))
                dllPath = Path.Combine(PlatformHelper.AppPath, "costura64", "serial_monitor.dll");
            if (!File.Exists(dllPath))
                dllPath = Path.Combine(PlatformHelper.AppPath, "costura32", "serial_monitor.dll");

            if (File.Exists(dllPath))
            {
                _nativeLib = NativeLibrary.Load(dllPath);
            }
            else
            {
                // Try system path
                try { _nativeLib = NativeLibrary.Load("serial_monitor"); }
                catch { }
            }
        }
        else if (PlatformHelper.IsLinux)
        {
            // Linux: load from native/linux-x64/
            var soPath = Path.Combine(PlatformHelper.AppPath, "native", "linux-x64", "libserial_monitor.so");
            if (File.Exists(soPath))
            {
                _nativeLib = NativeLibrary.Load(soPath);
            }
            else
            {
                try { _nativeLib = NativeLibrary.Load("libserial_monitor"); }
                catch { }
            }
        }
        else if (PlatformHelper.IsMacOS)
        {
            var dylibPath = Path.Combine(PlatformHelper.AppPath, "native", "osx-x64", "libserial_monitor.dylib");
            if (File.Exists(dylibPath))
            {
                _nativeLib = NativeLibrary.Load(dylibPath);
            }
        }

        if (_nativeLib == IntPtr.Zero)
        {
            AvailabilityMessage = PlatformHelper.IsWindows
                ? "串口监听功能需要 serial_monitor.dll，请确保文件存在。"
                : "串口监听功能在 Linux/macOS 上暂不支持。";
            return;
        }

        // Bind exported functions
        if (NativeLibrary.TryGetExport(_nativeLib, "MonitorComm", out var monitorPtr))
            _monitorComm = Marshal.GetDelegateForFunctionPointer<MonitorCommDelegate>(monitorPtr);

        if (NativeLibrary.TryGetExport(_nativeLib, "UnMonitorComm", out var unmonitorPtr))
            _unMonitorComm = Marshal.GetDelegateForFunctionPointer<UnMonitorCommDelegate>(unmonitorPtr);

        if (_monitorComm == null || _unMonitorComm == null)
        {
            NativeLibrary.Free(_nativeLib);
            _nativeLib = IntPtr.Zero;
            AvailabilityMessage = "serial_monitor 导出的函数不完整。";
        }
    }

    /// <summary>Start monitoring a COM port in a target process.</summary>
    public static bool MonitorComm(uint pid, uint comIndex, MonitorCallback callback)
    {
        if (_monitorComm == null) return false;
        return _monitorComm(pid, comIndex, callback);
    }

    /// <summary>Stop serial port monitoring.</summary>
    public static bool UnMonitorComm()
    {
        if (_unMonitorComm == null) return false;
        return _unMonitorComm();
    }
}
