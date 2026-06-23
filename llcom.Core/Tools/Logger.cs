using Serilog;
using Serilog.Events;

namespace llcom.Tools;

/// <summary>
/// Cross-platform logging system using Serilog.
/// Supports UART data logs and Lua script logs with timestamps.
/// </summary>
public static class Logger
{
    private static Serilog.Core.Logger? _uartLogger;
    private static Serilog.Core.Logger? _luaLogger;
    private static readonly object _uartLock = new();
    private static readonly object _luaLock = new();
    private static string? _uartLogFile;
    private static bool _disableLog;

    public static bool DisableLog
    {
        get => _disableLog;
        set
        {
            _disableLog = value;
            if (value) CloseUartLog();
        }
    }

    /// <summary>Data display callback (set by UI layer).</summary>
    public static Action<DataShowRaw>? ShowDataRawCallback { get; set; }

    public static void AddUartLogInfo(string message)
    {
        if (_disableLog) return;
        EnsureUartLogger();
        _uartLogger?.Information("{Message}", message);
    }

    public static void AddUartLogDebug(string message)
    {
        if (_disableLog) return;
        EnsureUartLogger();
        _uartLogger?.Debug("{Message}", message);
    }

    public static void AddLuaLog(string message)
    {
        EnsureLuaLogger();
        _luaLogger?.Information("{Message}", message);
    }

    public static void ShowDataRaw(DataShowRaw data)
    {
        ShowDataRawCallback?.Invoke(data);
    }

    private static void EnsureUartLogger()
    {
        lock (_uartLock)
        {
            if (_uartLogger != null) return;
            var logDir = Path.Combine(PlatformHelper.ProfilePath, "logs");
            Directory.CreateDirectory(logDir);
            _uartLogFile = Path.Combine(logDir,
                $"uart_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            _uartLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(_uartLogFile,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message}{NewLine}")
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();
        }
    }

    private static void EnsureLuaLogger()
    {
        lock (_luaLock)
        {
            if (_luaLogger != null) return;
            var logDir = Path.Combine(PlatformHelper.ProfilePath, "user_script_run", "logs");
            Directory.CreateDirectory(logDir);

            _luaLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logDir,
                    $"lua_{DateTime.Now:yyyy-MM-dd}.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message}{NewLine}")
                .CreateLogger();
        }
    }

    public static void CloseUartLog()
    {
        lock (_uartLock)
        {
            _uartLogger?.Dispose();
            _uartLogger = null;
        }
    }

    public static void CloseLuaLog()
    {
        lock (_luaLock)
        {
            _luaLogger?.Dispose();
            _luaLogger = null;
        }
    }

    public static void CloseAll()
    {
        CloseUartLog();
        CloseLuaLog();
    }
}

/// <summary>Data structure for displaying raw data in the UI.</summary>
public class DataShowRaw
{
    public string? title;
    public byte[]? data;
    public object? color;
}
