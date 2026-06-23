using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using llcom.Avalonia.ViewModels;
using llcom.Avalonia.Views;
using llcom.Tools;

namespace llcom.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize cross-platform abstractions
            PlatformHelper.ShowMessageCallback = (msg) =>
            {
                Console.WriteLine($"[llcom message] {msg}");
            };

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
