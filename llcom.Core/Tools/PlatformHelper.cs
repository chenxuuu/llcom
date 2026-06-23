using System.Diagnostics;
using System.Runtime.InteropServices;

namespace llcom.Tools;

/// <summary>
/// Cross-platform abstraction for platform-specific operations.
/// Provides OS-appropriate paths, message display, and file operations.
/// </summary>
public static class PlatformHelper
{
    /// <summary>Configuration/data profile directory (with trailing separator).</summary>
    public static string ProfilePath { get; set; } = GetDefaultProfilePath();

    /// <summary>Application root directory (with trailing separator).</summary>
    public static string AppPath { get; } = GetAppPath();

    /// <summary>Application executable file name.</summary>
    public static string FileName { get; } = GetFileName();

    /// <summary>Whether running on Windows.</summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>Whether running on Linux.</summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>Whether running on macOS.</summary>
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>Callback for showing message dialogs (set by UI layer).</summary>
    public static Action<string>? ShowMessageCallback { get; set; }

    /// <summary>Callback for loading language files (set by UI layer).</summary>
    public static Action<string>? LoadLanguageFileCallback { get; set; }

    /// <summary>Show a message to the user.</summary>
    public static void ShowMessage(string message)
    {
        ShowMessageCallback?.Invoke(message);
        // Fallback: write to console
        Console.WriteLine($"[llcom] {message}");
    }

    /// <summary>Load language resource file.</summary>
    public static void LoadLanguageFile(string language)
    {
        LoadLanguageFileCallback?.Invoke(language);
    }

    /// <summary>Get the path separator char for the current OS.</summary>
    public static char Sep => Path.DirectorySeparatorChar;

    private static string GetAppPath()
    {
        using var processModule = Process.GetCurrentProcess().MainModule;
        var dir = Path.GetDirectoryName(processModule?.FileName) ?? ".";
        if (!dir.EndsWith(Sep.ToString()))
            dir += Sep;
        return dir;
    }

    private static string GetFileName()
    {
        using var processModule = Process.GetCurrentProcess().MainModule;
        return Path.GetFileName(processModule?.FileName) ?? "llcom";
    }

    private static string GetDefaultProfilePath()
    {
        if (IsWindows)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "llcom") + Sep;
        }
        else if (IsMacOS)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "llcom") + Sep;
        }
        else // Linux
        {
            // XDG_CONFIG_HOME or ~/.config
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
                return Path.Combine(xdgConfig, "llcom") + Sep;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "llcom") + Sep;
        }
    }
}
