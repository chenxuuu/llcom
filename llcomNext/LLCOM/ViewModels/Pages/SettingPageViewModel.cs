using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LLCOM.ViewModels;

public partial class SettingPageViewModel : ViewModelBase
{
    private Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public SettingPageViewModel() {}
    
    public SettingPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
    }

    [ObservableProperty] private string _systemInfo =
        $"LLCOM: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\n\n" +
        $"Operating System: {Environment.OSVersion.VersionString}\n" +
        $".Net CLR: {Environment.Version}\n" +
#if debug
                                 $"Debug mode: true\n" +
#else
        $"Debug mode: false\n" +
#endif
        $"Location: {Environment.CurrentDirectory}\n" +
        $"AppData: TODO\n" +
        $"Environment: {Environment.GetEnvironmentVariable("PATH")}";

    [RelayCommand]
    private async Task CopySystemInfo()
    {
        await Services.Utils.CopyString(SystemInfo);
    }


}