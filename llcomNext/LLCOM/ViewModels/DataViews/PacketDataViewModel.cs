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
                    PacketData.Add(new ("0123"u8.ToArray(), MessageWay.Send, "串口1"));
                    PacketData.Add(new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"));
                }
            }
        });
    }
    
    [ObservableProperty]
    private ObservableCollection<PackData> _packetData =         [
        new ([], MessageWay.Unknown, "MQTT1",null,null,true,"已连接"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Send, "串口1"),
        new ("0123"u8.ToArray(), MessageWay.Receive, "串口1"),
    ];
    
    //自动滚到底部
    [ObservableProperty]
    private bool _autoScroll = true;
}