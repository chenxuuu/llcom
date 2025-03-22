using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace LLCOM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsDataPageActive),
        nameof(IsToolsPageActive),
        nameof(IsScriptPageActive),
        nameof(IsOnlinePageActive),
        nameof(IsLogPageActive),
        nameof(IsSettingPageActive)
        )]
    private ViewModelBase _currentPage;
    
    public bool IsDataPageActive => CurrentPage is DataPageViewModel;
    public bool IsToolsPageActive => CurrentPage is ToolsPageViewModel;
    public bool IsScriptPageActive => CurrentPage is ScriptPageViewModel;
    public bool IsOnlinePageActive => CurrentPage is OnlinePageViewModel;
    public bool IsLogPageActive => CurrentPage is LogPageViewModel;
    public bool IsSettingPageActive => CurrentPage is SettingPageViewModel;
    
    //用于设计时预览，正式代码中无效
    public MainViewModel() {}
    
    public MainViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
        CurrentPage = _getService(typeof(DataPageViewModel));
        CurrentDataPage = _getService(typeof(PacketDataViewModel));
    }

    [RelayCommand]
    private async Task PageLoaded()
    {
        //检查下是否有新版本吧
        if (await Services.Utils.CheckUpdate() != null)
            ShowSettingDot = true;
    }

    [RelayCommand]
    private void DataPageButton() => CurrentPage = _getService(typeof(DataPageViewModel));
    [RelayCommand]
    private void ToolsPageButton() => CurrentPage = _getService(typeof(ToolsPageViewModel));
    [RelayCommand]
    private void ScriptPageButton() => CurrentPage = _getService(typeof(ScriptPageViewModel));
    [RelayCommand]
    private void OnlinePageButton() => CurrentPage = _getService(typeof(OnlinePageViewModel));
    [RelayCommand]
    private void LogPageButton() => CurrentPage = _getService(typeof(LogPageViewModel));
    [RelayCommand]
    private void SettingPageButton() => CurrentPage = _getService(typeof(SettingPageViewModel));


    [ObservableProperty] 
    private bool _showSettingDot = false;
    
    //以下代码用于切换DataPage中的子页面
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPacketDataActive), nameof(IsTerminalActive), nameof(IsWaveformActive))]
    private ViewModelBase _currentDataPage;
    
    public bool IsPacketDataActive => CurrentDataPage is PacketDataViewModel;
    public bool IsTerminalActive => CurrentDataPage is TerminalViewModel;
    public bool IsWaveformActive => CurrentDataPage is WaveformViewModel;
    
    [RelayCommand]
    private void PacketDataButton() => CurrentDataPage = _getService(typeof(PacketDataViewModel));
    [RelayCommand]
    private void TerminalButton() => CurrentDataPage = _getService(typeof(TerminalViewModel));
    [RelayCommand]
    private void WaveformButton() => CurrentDataPage = _getService(typeof(WaveformViewModel));

    
}
