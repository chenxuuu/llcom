using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Model;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class OnlineScriptsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<OnlineScript> _scripts = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMsg = "加载中...";

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private bool _isIndeterminate = true;

    [ObservableProperty]
    private bool _isInList = true;

    [ObservableProperty]
    private OnlineScript? _selectedScript;

    public OnlineScriptsViewModel()
    {
        _ = RefreshListAsync();
    }

    [RelayCommand]
    private async Task RefreshList()
    {
        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        IsLoading = true;
        LoadingMsg = "正在加载在线脚本...";
        Scripts.Clear();

        var result = await Task.Run(() =>
            GlobalState.GetOnlineScripts((got, total) =>
            {
                LoadingMsg = $"加载中... {got}/{total}";
                Progress = (int)(got * 100.0 / total);
                IsIndeterminate = false;
            }));

        Scripts = new ObservableCollection<OnlineScript>(result);

        IsLoading = false;
        IsInList = true;
    }

    [RelayCommand]
    private void OpenScriptDetail(OnlineScript? script)
    {
        if (script == null) return;
        SelectedScript = script;
        IsInList = false;
    }

    [RelayCommand]
    private void BackToList()
    {
        IsInList = true;
    }

    [RelayCommand]
    private void OpenScriptUrl()
    {
        if (SelectedScript?.Url != null)
            PlatformHelper.OpenUrl(SelectedScript.Url);
    }

    [RelayCommand]
    private void OpenDiscussionPage()
    {
        PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/discussions/87");
    }

    [RelayCommand]
    private async Task DownloadScript()
    {
        if (SelectedScript == null) return;

        var name = SelectedScript.Name;
        var path = Path.Combine(PlatformHelper.ProfilePath, "user_script_run", $"{name}.lua");

        if (File.Exists(path))
        {
            PlatformHelper.ShowMessage("脚本文件已存在！");
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(PlatformHelper.ProfilePath, "user_script_run"));
            await File.WriteAllTextAsync(path, SelectedScript.Script);
            GlobalState.RefreshLuaScriptList();
            PlatformHelper.ShowMessage("保存成功！");
        }
        catch (Exception ex)
        {
            PlatformHelper.ShowMessage($"保存失败: {ex.Message}");
        }
    }
}
