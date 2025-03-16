using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LLCOM.Models;

namespace LLCOM.ViewModels;

public partial class PacketDataViewModel : ViewModelBase
{
    private Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public PacketDataViewModel() {}
    
    public PacketDataViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
        

    }
    
    [ObservableProperty]
    private ObservableCollection<PacketData> _packetData =         [
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Unknown, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
    ];
}