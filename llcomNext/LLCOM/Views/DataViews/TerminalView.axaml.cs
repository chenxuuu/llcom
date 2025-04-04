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

namespace LLCOM.Views;

public partial class TerminalView : UserControl
{
    public TerminalView()
    {
        InitializeComponent();
        MainArea.PropertyChanged += MainArea_PropertyChanged;
    }
    
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        Utils.Setting.TerminalChangedEvent += (sender, args) =>
        {
            //在UI线程中执行
            Dispatcher.UIThread.Post(() =>
            {
                //字体改变了，重新计算宽高
                (WindowWidth, WindowHeight) = Utils.CalculateSize(
                    MainArea.Bounds.Width, MainArea.Bounds.Height,
                    Utils.Setting.TerminalFont, Utils.Setting.TerminalFontSize);
                //TEST
                // var line = new List<TerminalBlock>();
                // line.Add(new TerminalBlock(new string('A',WindowWidth), 0, 0, false, false, false));
                // AddLine(line);
                //TEST END
                RefreshText();
            });
        };
        
        for(int i=0;i<200;i++)
        {
            var line = new List<TerminalBlock>();
            line.Add(new TerminalBlock($"test string {i} line.", 0, 0, false, false, false));
            line.Add(new TerminalBlock($"balabala", 0, 31, false, false, false));
            AddLine(line);
        }
        RefreshText();
    }
    
    //终端的宽高，是指可以存放的行数和列数
    private int WindowWidth { get; set; } = 1;
    private int WindowHeight { get; set; } = 1;
    
    //用于存放终端数据的缓存
    private List<List<TerminalBlock>> CacheLines { get; set; } = new();
    //当前所在的行数相比较于终端最底部的行数，0表示在最底部，其余数字表示向上挪动的行数
    private int CurrentLine { get; set; } = 0;

    private void AddLine(List<TerminalBlock> line)
    {
        CacheLines.Add(line);
        //检查是否超过最大行数
        if (CacheLines.Count > Utils.Setting.TerminalBufferLines)
        {
            CacheLines.RemoveAt(0);
        }
        //如果不是最底部，则可视范围向上移动
        if (CurrentLine != 0)
            MoveUp(1);
        
        //TODO))
        //RefreshText();
    }
    //向上移动的行数
    private bool MoveUp(int delta)
    {
        var lastCurrentLine = CurrentLine;
        CurrentLine += delta;
        if(CurrentLine > CacheLines.Count - WindowHeight)
            CurrentLine = CacheLines.Count - WindowHeight;
        if(CurrentLine < 0)
            CurrentLine = 0;
        return lastCurrentLine != CurrentLine;
    }    
    //滚轮事件
    private void MainArea_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if(MoveUp((int)e.Delta.Y * 3))
            RefreshText();
    }
    
    
    private void MainArea_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            double newWidth = MainArea.Bounds.Width;
            double newHeight = MainArea.Bounds.Height;
            Debug.WriteLine($"New width: {newWidth}, New height: {newHeight}");

            (WindowWidth, WindowHeight) = Utils.CalculateSize(
                newWidth, newHeight,
                Utils.Setting.TerminalFont, Utils.Setting.TerminalFontSize);

            RefreshText();
        }
    }
    
    /// <summary>
    /// 刷新展示区域的文本
    /// </summary>
    private void RefreshText()
    {
        //计算出要显示的行数范围
        int allLines = CacheLines.Count;
        //起始行和结束行，闭区间，从0开始，代表CacheLines的项目下标
        int startLine = allLines - WindowHeight - CurrentLine;
        if (startLine < 0)
            startLine = 0;
        int endLine = startLine + WindowHeight - 1;
        if (endLine >= allLines)
            endLine = allLines - 1;
        //清空文本
        MainTextBlock.Inlines!.Clear();
        //添加行
        for (int i = startLine; i <= endLine; i++)
        {
            //添加行
            var line = CacheLines[i];
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
    }

    private void MainScrollBar_OnScroll(object? sender, ScrollEventArgs e)
    {
        var value = e.NewValue;
        //计算出要显示的行数范围
        //TODO)) 还要关联上CurrentLine的变化
    }
}