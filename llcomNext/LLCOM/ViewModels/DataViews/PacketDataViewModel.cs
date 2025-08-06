using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLCOM.Models;

namespace LLCOM.ViewModels;

public partial class PacketDataViewModel : ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public PacketDataViewModel() {}
    
    public PacketDataViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;

        //添加数据包到分包数据界面的操作
        Services.Utils.AddPacketDataAction = data =>
        {
            lock (PacketData)
                PacketData.Add(data);
            if(AutoScroll)
                ScrollToBottomAction?.Invoke();
        };
    }
    
    [ObservableProperty]
    private ObservableCollection<PackData> _packetData = [];
    
    //自动滚到底部
    [ObservableProperty]
    private bool _autoScroll = true;
    
    public Action? ScrollToBottomAction { get; set; }
    
    [RelayCommand]
    private async Task ClearPacketData()
    {
        await Task.Run(() =>
        {
            lock (PacketData)
                PacketData.Clear();
        });
    }
}