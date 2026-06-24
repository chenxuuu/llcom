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

        // Sanitize the script name to prevent path traversal
        var name = SelectedScript.Name
            ?.Replace("/", "").Replace("\\", "").Replace("..", "")
            ?? "untitled";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        if (string.IsNullOrWhiteSpace(name)) name = "untitled";

        var baseDir = Path.GetFullPath(Path.Combine(PlatformHelper.ProfilePath, "user_script_run"));
        var path = Path.GetFullPath(Path.Combine(baseDir, $"{name}.lua"));
        if (!path.StartsWith(baseDir))
        {
            PlatformHelper.ShowMessage("无法保存: 无效的脚本名称");
            return;
        }

        if (File.Exists(path))
        {
            PlatformHelper.ShowMessage(LocaleHelper.Get("OnlineScriptFileExists"));
            return;
        }

        try
        {
            Directory.CreateDirectory(baseDir);
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
