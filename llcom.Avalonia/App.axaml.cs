using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
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

            // Set clipboard callback for EncodingFixViewModel
            EncodingFixViewModel.CopyToClipboardCallback = async (text) =>
            {
                if (desktop.MainWindow is { } window)
                {
                    var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
                    if (clipboard != null)
                        await clipboard.SetTextAsync(text);
                }
            };

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Language switching: swap the merged ResourceDictionary at runtime
            PlatformHelper.LoadLanguageFileCallback = (language) =>
            {
                if (desktop.MainWindow is MainWindow mainWindow)
                {
                    var dict = mainWindow.Resources;
                    dict.MergedDictionaries.Clear();
                    dict.MergedDictionaries.Add(new ResourceInclude(default(Uri))
                    {
                        Source = new Uri($"avares://llcom/languages/{language}.xaml")
                    });
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
