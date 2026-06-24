using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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

    /// <summary>Callback for showing input dialogs (set by UI layer).</summary>
    public static Func<string, string, string, (bool, string)>? InputDialogCallback { get; set; }

    /// <summary>Callback for opening file picker (set by UI layer). Returns selected file path or null.</summary>
    public static Func<string, Task<string?>>? OpenFilePickerCallback { get; set; }

    /// <summary>Callback for saving file picker (set by UI layer). Returns save path or null.</summary>
    public static Func<string, string, Task<string?>>? SaveFilePickerCallback { get; set; }

    /// <summary>Show a message to the user.</summary>
    public static void ShowMessage(string message)
    {
        ShowMessageCallback?.Invoke(message);
        // Fallback: write to console
        Console.WriteLine($"[llcom] {message}");
    }

    /// <summary>Show an input dialog to the user. Returns (confirmed, inputText).</summary>
    public static (bool, string) ShowInputDialog(string prompt, string defaultInput, string title)
    {
        if (InputDialogCallback != null)
            return InputDialogCallback(prompt, defaultInput, title);
        // Fallback: return default
        return (false, defaultInput);
    }

    /// <summary>Load language resource file.</summary>
    public static void LoadLanguageFile(string language)
    {
        LoadLanguageFileCallback?.Invoke(language);
    }

    /// <summary>Get the path separator char for the current OS.</summary>
    public static char Sep => Path.DirectorySeparatorChar;

    /// <summary>Get a human-readable platform name.</summary>
    public static string GetPlatformName()
    {
        if (IsWindows) return "Windows";
        if (IsMacOS) return "macOS";
        if (IsLinux) return "Linux";
        return "Unknown";
    }

    /// <summary>Open a URL in the default browser.</summary>
    public static void OpenUrl(string url)
    {
        // Validate URL to prevent command injection
        if (string.IsNullOrEmpty(url)) return;
        // Only allow http/https/file URLs or system paths (for opening folders)
        var isUrl = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
        var isPath = Path.IsPathRooted(url) || url.Contains(Sep);
        if (!isUrl && !isPath) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback for Linux where UseShellExecute might fail
            try
            {
                // Use safe argument passing via ProcessStartInfo ArgumentList (no shell injection)
                if (isUrl)
                {
                    Process.Start(new ProcessStartInfo("xdg-open", url)
                    {
                        UseShellExecute = false
                    });
                }
                else
                {
                    // For file paths, open via default file manager
                    var opener = IsWindows ? "explorer.exe" : (IsMacOS ? "open" : "xdg-open");
                    Process.Start(new ProcessStartInfo(opener, url)
                    {
                        UseShellExecute = false
                    });
                }
            }
            catch { }
        }
    }

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
