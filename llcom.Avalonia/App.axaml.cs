using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform.Storage;
using llcom.Avalonia.Helpers;
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

            // Input dialog callback (for custom baud rate, list rename, etc.)
            PlatformHelper.InputDialogCallback = (prompt, defaultInput, title) =>
            {
                return ShowInputDialogSync(desktop.MainWindow!, prompt, defaultInput, title);
            };

            // File picker callbacks
            PlatformHelper.OpenFilePickerCallback = async (filter) =>
            {
                return await OpenFilePickerAsync(desktop.MainWindow!, filter);
            };
            PlatformHelper.SaveFilePickerCallback = async (filter, defaultName) =>
            {
                return await SaveFilePickerAsync(desktop.MainWindow!, filter, defaultName);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static (bool, string) ShowInputDialogSync(Window owner, string prompt, string defaultInput, string title)
    {
        var tcs = new TaskCompletionSource<(bool, string)>();
        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel == null) return (false, defaultInput);

        var window = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false,
            CanResize = false,
        };

        var panel = new StackPanel { Margin = new global::Avalonia.Thickness(10) };
        var promptText = new TextBlock { Text = prompt, Margin = new global::Avalonia.Thickness(0, 0, 0, 8) };
        var inputBox = new TextBox { Text = defaultInput, Margin = new global::Avalonia.Thickness(0, 0, 0, 12) };
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var okBtn = new Button { Content = LocaleHelper.Get("InputDialogConfirm"), Width = 70, Margin = new global::Avalonia.Thickness(0, 0, 8, 0) };
        var cancelBtn = new Button { Content = LocaleHelper.Get("InputDialogCancel"), Width = 70 };

        okBtn.Click += (_, _) => { tcs.TrySetResult((true, inputBox.Text ?? "")); window.Close(); };
        cancelBtn.Click += (_, _) => { tcs.TrySetResult((false, defaultInput)); window.Close(); };
        window.Closing += (_, _) => tcs.TrySetResult((false, defaultInput));

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(promptText);
        panel.Children.Add(inputBox);
        panel.Children.Add(btnPanel);
        window.Content = panel;

        window.Show(owner);
        inputBox.Focus();
        inputBox.SelectAll();

        // Sync wait with message pump
        var result = Task.Run(() => tcs.Task).Result;
        return result;
    }

    private static async Task<string?> OpenFilePickerAsync(Window owner, string filter)
    {
        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel == null) return null;

        var filters = ParseFilters(filter);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false,
            FileTypeFilter = filters,
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private static async Task<string?> SaveFilePickerAsync(Window owner, string filter, string defaultName)
    {
        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel == null) return null;

        var filters = ParseFilters(filter);
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File",
            SuggestedFileName = defaultName,
            FileTypeChoices = filters,
        });

        return file?.Path.LocalPath;
    }

    private static System.Collections.Generic.List<FilePickerFileType> ParseFilters(string filter)
    {
        var result = new System.Collections.Generic.List<FilePickerFileType>();
        // format: "Description|*.ext;*.ext|Description2|*.ext2"
        var parts = filter.Split('|');
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            var name = parts[i];
            var patterns = parts[i + 1].Split(';').Select(p => p.Trim().TrimStart('*')).ToList();
            result.Add(new FilePickerFileType(name)
            {
                Patterns = patterns,
            });
        }
        return result;
    }
}
