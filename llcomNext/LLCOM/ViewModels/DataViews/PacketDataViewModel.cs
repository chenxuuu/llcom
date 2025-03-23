using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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

        Task.Run(async () =>
        {
            for(int i=0; i<100; i++)
            {
                await Task.Delay(100);
                for(int j=0; j<100; j++)
                {
                    PacketData.Add(new PacketData(new byte[]{0x30, 0x31, 0x32, 0x33}, MessageWay.Send, "串口1"));
                    PacketData.Add(new PacketData(new byte[]{0x30, 0x31, 0x32, 0x33}, MessageWay.Receive, "串口1"));
                }
            }
        });
    }
    
    [ObservableProperty]
    private ObservableCollection<PacketData> _packetData =         [
        new PacketData([], MessageWay.Unknown, "MQTT1",null,null,true,"已连接"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Send, "串口1"),
        new PacketData([0x30, 0x31, 0x32, 0x33], MessageWay.Receive, "串口1"),
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
    
    //自动滚到底部
    [ObservableProperty]
    private bool _autoScroll = true;
}