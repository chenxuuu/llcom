using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
        MainWindowViewModel.OpenSettingsRequested += ShowSettingsWindow;
    }

    private void ShowSettingsWindow()
    {
        var settingVm = new SettingWindowViewModel();
        var settingWindow = new SettingWindow { DataContext = settingVm };
        settingWindow.Show(this);
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

    // ── Terminal mode: forward keyboard input to serial port ─────────────

    private void DataListBox_GotFocus(object? sender, global::Avalonia.Input.GotFocusEventArgs e)
    {
        if (DataContext is MainWindowViewModel { TerminalMode: true } vm)
        {
            vm.TerminalBorderColor = "#009400";
            dataShowBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 148, 0));
        }
    }

    private void DataListBox_LostFocus(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.TerminalBorderColor = "Transparent";
            dataShowBorder.BorderBrush = Brushes.Transparent;
        }
    }

    private void DataListBox_TextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text) || DataContext is not MainWindowViewModel vm) return;
        vm.HandleTerminalKeyInput(e.Text);
        e.Handled = true;
    }

    private void DataListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        // Ctrl+A..Z sends ASCII control codes
        var hasCtrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        if (hasCtrl && e.Key >= Key.A && e.Key <= Key.Z)
        {
            var asciiCode = (int)e.Key - (int)Key.A;
            vm.HandleTerminalCtrlKey(asciiCode);
            e.Handled = true;
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
