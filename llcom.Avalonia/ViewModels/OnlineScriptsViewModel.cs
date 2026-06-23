using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Avalonia.Helpers;
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
    private string _loadingMsg = LocaleHelper.Get("Loading");

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
        LoadingMsg = LocaleHelper.Get("LoadingOnlineScripts");
        Scripts.Clear();

        var result = await Task.Run(() =>
            GlobalState.GetOnlineScripts((got, total) =>
            {
                LoadingMsg = LocaleHelper.Format("LoadingProgress", got, total);
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
            PlatformHelper.ShowMessage(LocaleHelper.Get("OnlineScriptFileExists"));
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(PlatformHelper.ProfilePath, "user_script_run"));
            await File.WriteAllTextAsync(path, SelectedScript.Script);
            GlobalState.RefreshLuaScriptList();
            PlatformHelper.ShowMessage(LocaleHelper.Get("OnlineScriptSaveSuccess"));
        }
        catch (Exception ex)
        {
            PlatformHelper.ShowMessage(LocaleHelper.Format("OnlineScriptSaveFailed", ex.Message));
        }
    }
}
