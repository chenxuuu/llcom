using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using llcom.Avalonia.ViewModels;
using llcom.Tools;

namespace llcom.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadWindowPosition();
        Closed += SaveWindowPosition;
        Activated += OnWindowActivatedEvent;
        Deactivated += OnWindowDeactivatedEvent;
    }

    private void OnWindowActivatedEvent(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.LuaScriptPage.OnWindowActivated();
        }
    }

    private void OnWindowDeactivatedEvent(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.LuaScriptPage.OnEditorLostFocus();
        }
    }

    private void LoadWindowPosition()
    {
        try
        {
            var path = Path.Combine(PlatformHelper.ProfilePath, "window_state.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<WindowPosState>(json);
                if (state != null && state.Left > 0 && state.Top > 0)
                {
                    if (state.Width >= 400 && state.Height >= 300)
                    {
                        Position = new global::Avalonia.PixelPoint((int)state.Left, (int)state.Top);
                        Width = state.Width;
                        Height = state.Height;
                    }
                }
            }
        }
        catch { /* ignore */ }
    }

    private void SaveWindowPosition(object? sender, EventArgs e)
    {
        try
        {
            var state = new WindowPosState
            {
                Left = Position.X,
                Top = Position.Y,
                Width = Width,
                Height = Height
            };
            var path = Path.Combine(PlatformHelper.ProfilePath, "window_state.json");
            Directory.CreateDirectory(PlatformHelper.ProfilePath);
            File.WriteAllText(path, JsonSerializer.Serialize(state));
        }
        catch { /* ignore */ }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.LuaScriptPage.OnEditorLostFocus();
            vm.Cleanup();
        }
    }

    private class WindowPosState
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
