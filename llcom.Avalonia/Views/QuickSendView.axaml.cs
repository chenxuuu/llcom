using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
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

    /// <summary>Handle click on 🛠 icon to set recv script parameters.</summary>
    private void ScriptParaIcon_PointerPressed(object? sender, PointerPressedEventArgs e)
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

        // Left-click: show input dialog for script parameters
        var result = PlatformHelper.ShowInputDialog(
            "设置接收脚本参数:",
            item.RecvScriptPara ?? "",
            "脚本参数");
        if (result.Item1)
        {
            item.RecvScriptPara = result.Item2;
        }
        e.Handled = true;
    }
}
