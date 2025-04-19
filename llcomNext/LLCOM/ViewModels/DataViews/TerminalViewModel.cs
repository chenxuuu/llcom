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
            if(TerminalChangedEvent == null)
                return;
            //更新数据
            TerminalChangedEvent?.Invoke(this, args);
        };
    }

    [RelayCommand]
    private async Task Test()
    {
        var random = new Random();
        var testChars = "测试Test".ToCharArray();
        for(int i=0; i<1000; i++)
        {
            lock (TerminalObject)
            {
                TerminalObject.ChangeStyle(random.Next(30,38),random.Next(30,38));
                TerminalObject.ChangePosition(random.Next(0,TerminalObject.WindowWidth), random.Next(0,TerminalObject.WindowHeight));
                TerminalObject.AddText([testChars[random.Next(0, testChars.Length)]]); 
                TerminalChangedEvent?.Invoke(this, TerminalObject.GetShowLines());
            }
            await Task.Delay(1);
        }
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
    public EventHandler<List<List<TerminalBlock>>>? TerminalChangedEvent;
}