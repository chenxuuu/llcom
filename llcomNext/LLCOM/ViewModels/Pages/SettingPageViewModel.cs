using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestSharp;

namespace LLCOM.ViewModels;

public partial class SettingPageViewModel : ViewModelBase
{
    private Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public SettingPageViewModel() {}
    
    public SettingPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;

        //初始化系统信息
        Task.Run(() =>
        {
            var packagesInfo = new List<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
            foreach (var assembly in assemblies)
            {
                var name = assembly.GetName();
                packagesInfo.Add($"{name.Name}: {name.Version}");
            }
            
            SystemInfo =
                $"LLCOM: {Services.Utils.Version}\n" +
                $"OS: {Environment.OSVersion.VersionString}\n" +
                $".Net CLR: {Environment.Version}\n" +
#if debug
                $"Debug mode: true\n" +
#else
                $"Debug mode: false\n" +
#endif
                $"Location: {Environment.CurrentDirectory}\n" +
                $"AppData: {Services.Utils.AppPath}\n" +
                $"Packages \n{string.Join("\n", packagesInfo)}\n" +
                $"Environment: {Environment.GetEnvironmentVariable("PATH")}";
        });
    }

    
    #region About
    [ObservableProperty] private string _systemInfo = "Loading...";

    [RelayCommand]
    private async Task CopySystemInfo()
    {
        if(SystemInfo.Length < 20)//还没加载完
            return;
        await Services.Utils.CopyString(SystemInfo);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNewVersion), nameof(NoNewVersion))]
    private bool _hasCheckedUpdate = false;
    
    public bool HasNewVersion => HasCheckedUpdate && _updateUrl != null;
    public bool NoNewVersion => HasCheckedUpdate && _updateUrl == null;
    
    private string? _updateUrl = null;
    
    [RelayCommand]
    private async Task CheckUpdate()
    {
        if (HasCheckedUpdate && _updateUrl != null)
        {
            Services.Utils.OpenWebLink(_updateUrl);
            return;
        }

        var versionNow = Services.Utils.Version;
        //获取https://api.github.com/repos/chenxuuu/llcom/releases/latest的json结果
        var client = new RestClient("https://api.github.com/repos/chenxuuu/llcom/releases/latest");
        var request = new RestRequest();
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            return;
        
        var json = System.Text.Json.JsonDocument.Parse(response.Content);
        var version = json.RootElement.GetProperty("tag_name").GetString();
        if(version == null)
            return;
        
        //判断在线版本是否新于本地版本，按点分割后比较
        var isBigger = true;
        if(version == versionNow)
            isBigger = false;
        else
        {
            var versionNowSplit = versionNow.Split('.');
            var versionSplit = version.Split('.');
            for (int i = 0; i < versionNowSplit.Length; i++)
            {
                if (int.Parse(versionNowSplit[i]) > int.Parse(versionSplit[i]))
                {
                    isBigger = false;
                    break;
                }
            }
        }

        //更新信息
        if(isBigger)
            _updateUrl = json.RootElement.GetProperty("html_url").GetString();
        
        HasCheckedUpdate = true;
    }
    #endregion

}