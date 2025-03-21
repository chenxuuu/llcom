using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LLCOM.ViewModels;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace LLCOM.Views;

public partial class MainView : UserControl
{
    private WindowNotificationManager? _manager;
    
    public MainView()
    {
        InitializeComponent();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        _manager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        await ((MainViewModel)this.DataContext!).PageLoadedCommand.ExecuteAsync(null);
        
        if(Services.Utils.HasUpdate())
            _manager?.Show(
                new Notification("提示", "有新版本可用，请前往关于页面查看"),
                type: NotificationType.Information,
                classes: ["Light"],
                expiration: TimeSpan.FromSeconds(60));
    }
}