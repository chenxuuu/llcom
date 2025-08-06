using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LLCOM.ViewModels;

namespace LLCOM.Views;

public partial class PacketDataView : UserControl
{
    
    public PacketDataView()
    {
        InitializeComponent();
    }

    private bool _needScrollToBottom = false;
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PacketDataViewModel vm)
            vm.ScrollToBottomAction = () => _needScrollToBottom = true;
        
        
        //开一个定时器，定时检查是否需要滚动到底部
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        timer.Tick += (_, args) =>
        {
            if (_needScrollToBottom)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (MainListBox.ItemCount > 0)
                        MainListBox.ScrollIntoView(MainListBox.ItemCount - 1);
                }, DispatcherPriority.Background);
                _needScrollToBottom = false;
            }
        };
        timer.Start();
    }
}