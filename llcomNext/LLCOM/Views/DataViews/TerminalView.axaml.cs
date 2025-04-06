using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using LLCOM.Models;
using LLCOM.Services;
using LLCOM.ViewModels;

namespace LLCOM.Views;

public partial class TerminalView : UserControl
{
    public TerminalView()
    {
        InitializeComponent();
        MainArea.PropertyChanged += MainArea_PropertyChanged;
        Debug.WriteLine("TerminalView initialized.");
    }

    //被销毁后的事件
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        MainArea.PropertyChanged -= MainArea_PropertyChanged;
        Utils.Setting.TerminalChangedEvent -= TerminalChangedEvent;
        ((TerminalViewModel)DataContext!).TerminalChangedEvent -= TerminalChangedEvent;
        Debug.WriteLine("TerminalView unloaded.");
    }
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        Utils.Setting.TerminalChangedEvent += TerminalChangedEvent;
        ((TerminalViewModel)DataContext!).TerminalChangedEvent += TerminalChangedEvent;
        //加载完触发一次，顺便初始化窗口大小数据
        RefreshWindowSize();
    }
    
    private void MainArea_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        //尺寸改变了，重新计算宽高
        if (e.Property == BoundsProperty)
            RefreshWindowSize();
    }

    private void RefreshWindowSize()
    {
        var margin = MainTextBlock.Margin.Left;
        var (w, h) = Utils.CalculateSize(
            MainArea.Bounds.Width - margin * 2, MainArea.Bounds.Height - margin * 2,
            Utils.Setting.TerminalFont, Utils.Setting.TerminalFontSize);
        (DataContext as TerminalViewModel)?.ChangeWindowSize((w,h));
    }

    private void TerminalChangedEvent(object? sender, EventArgs e)
    {
        //在UI线程中执行
        Dispatcher.UIThread.Post(RefreshWindowSize);
    }
    
    //滚轮事件
    private void MainArea_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        MainScrollBar.Value = ((TerminalViewModel)DataContext!).MoveUp((int)e.Delta.Y * 3);
    }
    
    private void MainScrollBar_OnScroll(object? sender, ScrollEventArgs e)
    {
        ((TerminalViewModel)DataContext!).ScrollBarChanged(e.NewValue);
    }
    
    private void TerminalChangedEvent(object? sender, List<List<TerminalBlock>> e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            //清空文本
            MainTextBlock.Inlines!.Clear();
            foreach (var line in e)
            {
                foreach (var block in line)
                {
                    //添加块
                    var run = new Run(block.Text)
                    {
                        [!Span.ForegroundProperty] = new Binding(block.ForegroundBindingName) { Source = Utils.Setting },
                        [!Span.BackgroundProperty] = new Binding(block.BackgroundBindingName) { Source = Utils.Setting },
                        FontWeight = block.IsBold ? FontWeight.Bold : FontWeight.Normal,
                        FontStyle = block.IsItalic ? FontStyle.Italic : FontStyle.Normal,
                        TextDecorations = block.IsUnderLine ? TextDecorations.Underline : null,
                    };
                    MainTextBlock.Inlines.Add(run);
                }
                MainTextBlock.Inlines.Add(new LineBreak());
            }
        });
    }
}