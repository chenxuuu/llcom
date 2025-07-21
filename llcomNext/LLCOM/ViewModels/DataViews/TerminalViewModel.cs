using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLCOM.Models;
using LLCOM.Services;

namespace LLCOM.ViewModels;

public partial class TerminalViewModel : ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public TerminalViewModel() {}
    
    public TerminalViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
        
        TerminalObject.TerminalChangedEvent += (sender, args) =>
        {
            if(TerminalRefreshEvent == null)
                return;
            //更新数据
            TerminalRefreshEvent?.Invoke(this, args);
        };
    }

    [RelayCommand]
    private async Task Test()
    {
        //测试显示数据
        await Task.Run(() =>
        {
            lock (TerminalObject)
            {
                TerminalObject.AddChars("123456\b123456\b测试\b");
            }
        });
    }

    //终端对象，  TODO)) 后续需要为每项操作加锁
    public readonly TerminalObject TerminalObject = new TerminalObject();
    
    //窗口大小变化
    public void ChangeWindowSize((int, int) size)
    {
        lock(TerminalObject)
            TerminalObject.ChangeWindowSize(size.Item1, size.Item2);
    }
    
    //滚轮事件
    public double MoveUp(int delta)
    {
        lock(TerminalObject)
            return TerminalObject.CurrentLineMoveUp(delta);
    }
    
    //滚动条变化
    public void ScrollBarChanged(double value)
    {
        lock(TerminalObject)
            TerminalObject.ScrollBarChanged(value);
    }
    //接管更新事件
    public EventHandler? TerminalRefreshEvent;
    
    public List<List<TerminalBlock>> GetShowLines()
    {
        lock (TerminalObject)
        {
            var newLines = new List<List<TerminalBlock>>();
            var lines = TerminalObject.GetShowLines();
            foreach (var line in lines)
            {
                var newLine = new List<TerminalBlock>();
                foreach (var block in line)
                {
                    newLine.Add((TerminalBlock)block.Clone());
                }
                newLines.Add(newLine);
            }
            return newLines;
        }
    }
}