using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
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
            if(TerminalChangedEvent == null)
                return;
            //更新数据
            TerminalChangedEvent?.Invoke(this, args);
        };
    }

    [RelayCommand]
    private void Test()
    {
        TerminalObject.ChangePosition(0, 0);
        TerminalObject.AddText($"A".ToCharArray());
        TerminalObject.ChangePosition(20, 0);
        TerminalObject.AddText($"B".ToCharArray());
        TerminalObject.ChangePosition(0, 20);
        TerminalObject.AddText($"C".ToCharArray());
        TerminalObject.ChangePosition(20, 20);
        TerminalObject.AddText($"D".ToCharArray());
        
        TerminalChangedEvent?.Invoke(this, TerminalObject.GetShowLines());
    }

    //终端对象，  TODO)) 后续需要为每项操作加锁
    public readonly TerminalObject TerminalObject = new TerminalObject();
    
    //窗口大小变化
    public void ChangeWindowSize((int, int) size)
    {
        TerminalObject.ChangeWindowSize(size.Item1, size.Item2);
    }
    
    //滚轮事件
    public double MoveUp(int delta) => TerminalObject.CurrentLineMoveUp(delta);
    //滚动条变化
    public void ScrollBarChanged(double value) => TerminalObject.ScrollBarChanged(value);
    //接管更新事件
    public EventHandler<List<List<TerminalBlock>>>? TerminalChangedEvent;
}