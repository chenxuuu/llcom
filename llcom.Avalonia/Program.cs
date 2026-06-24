using Avalonia;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace llcom.Avalonia;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Register legacy encodings (GBK, Shift_JIS, etc.) for EncodingFix feature
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Global unhandled exception handlers for better diagnostics
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Console.Error.WriteLine($"[llcom FATAL] Unhandled exception: {ex}");
#if DEBUG
            if (ex != null)
                Console.Error.WriteLine(ex.ToString());
#endif
        };

        // Print environment diagnostics on Linux (help troubleshoot X11/Wayland issues)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var display = Environment.GetEnvironmentVariable("DISPLAY");
            var wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            var session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
            Console.WriteLine($"[llcom] DISPLAY={display ?? "(not set)"} WAYLAND_DISPLAY={wayland ?? "(not set)"} XDG_SESSION_TYPE={session ?? "(not set)"}");
        }

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            Console.Error.WriteLine($"[llcom FATAL] Startup failed: {msg}");

            // Give actionable hints for common Linux display failures
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                (msg.Contains("XOpenDisplay") || msg.Contains("Display") || msg.Contains("X11")))
            {
                Console.Error.WriteLine("[llcom HINT] X11 connection failed. Possible fixes:");
                Console.Error.WriteLine("  1. Ensure you are running from a desktop environment (not SSH).");
                Console.Error.WriteLine("  2. If using Wayland, install XWayland: sudo apt install xwayland");
                Console.Error.WriteLine("  3. Try: export DISPLAY=:0 && ./llcom");
            }

            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
