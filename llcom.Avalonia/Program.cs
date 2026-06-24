using Avalonia;
using System;

namespace llcom.Avalonia;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Register legacy encodings (GBK, Shift_JIS, etc.) for EncodingFix feature
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
