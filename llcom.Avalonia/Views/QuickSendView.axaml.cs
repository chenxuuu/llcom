using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using llcom.Avalonia.ViewModels;
using llcom.Tools;

namespace llcom.Avalonia.Views;

public partial class QuickSendView : UserControl
{
    public QuickSendView()
    {
        InitializeComponent();
    }

    /// <summary>Handle click on 📜 icon to select recv script.</summary>
    private void ScriptIcon_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control icon) return;
        if (icon.Tag is not QuickSendItem item) return;

        var point = e.GetCurrentPoint(icon);
        if (point.Properties.IsRightButtonPressed)
        {
            // Right-click: clear recv script
            item.RecvScriptPath = "";
            e.Handled = true;
            return;
        }

        // Left-click: show popup with script selection
        var scriptsDir = Path.Combine(PlatformHelper.ProfilePath, "user_script_recv_convert");
        var scripts = new List<string>();
        if (Directory.Exists(scriptsDir))
            scripts = Directory.GetFiles(scriptsDir, "*.lua")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(s => s)
                .ToList()!;

        if (scripts.Count == 0)
        {
            PlatformHelper.ShowMessage("暂无接收脚本（user_script_recv_convert/ 目录为空）");
            return;
        }

        var popup = new Popup
        {
            PlacementTarget = icon,
            Placement = PlacementMode.Bottom,
            IsLightDismissEnabled = true,
        };

        var comboBox = new ComboBox
        {
            ItemsSource = scripts,
            SelectedItem = !string.IsNullOrEmpty(item.RecvScriptPath) ? item.RecvScriptPath : null,
            Width = 180,
            Margin = new global::Avalonia.Thickness(4),
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is string selected)
            {
                item.RecvScriptPath = selected;
            }
            popup.IsOpen = false;
        };

        var border = new Border
        {
            Background = global::Avalonia.Media.Brushes.White,
            BorderBrush = global::Avalonia.Media.Brushes.Gray,
            BorderThickness = new global::Avalonia.Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = comboBox,
        };

        popup.Child = border;
        popup.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>
    /// Handle click on 🛠 icon to set recv script parameters.
    /// Uses async pattern to avoid UI thread deadlock.
    /// </summary>
    private async void ScriptParaIcon_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control icon) return;
        if (icon.Tag is not QuickSendItem item) return;

        var point = e.GetCurrentPoint(icon);
        if (point.Properties.IsRightButtonPressed)
        {
            // Right-click: clear recv script parameters
            item.RecvScriptPara = "";
            e.Handled = true;
            return;
        }

        // Left-click: show input dialog for script parameters (async, non-blocking)
        e.Handled = true;
        var result = await ShowInputDialogAsync(
            TopLevel.GetTopLevel(this),
            "设置接收脚本参数:",
            item.RecvScriptPara ?? "",
            "脚本参数");
        if (result.confirmed)
        {
            item.RecvScriptPara = result.text;
        }
    }

    /// <summary>
    /// Async input dialog that doesn't block the UI thread.
    /// Uses TaskCompletionSource with Show() instead of blocking .Result.
    /// </summary>
    private static Task<(bool confirmed, string text)> ShowInputDialogAsync(
        TopLevel? topLevel, string prompt, string defaultInput, string title)
    {
        var tcs = new TaskCompletionSource<(bool, string)>();
        if (topLevel == null)
        {
            tcs.TrySetResult((false, defaultInput));
            return tcs.Task;
        }

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

        var okBtn = new Button { Content = "确定", Width = 70, Margin = new global::Avalonia.Thickness(0, 0, 8, 0) };
        var cancelBtn = new Button { Content = "取消", Width = 70 };

        okBtn.Click += (_, _) => { tcs.TrySetResult((true, inputBox.Text ?? "")); window.Close(); };
        cancelBtn.Click += (_, _) => { tcs.TrySetResult((false, defaultInput)); window.Close(); };
        window.Closing += (_, _) => tcs.TrySetResult((false, defaultInput));

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(promptText);
        panel.Children.Add(inputBox);
        panel.Children.Add(btnPanel);
        window.Content = panel;

        var owner = topLevel as Window;
        if (owner != null)
            window.Show(owner);
        else
            window.Show();
        inputBox.Focus();
        inputBox.SelectAll();

        return tcs.Task;
    }
}
