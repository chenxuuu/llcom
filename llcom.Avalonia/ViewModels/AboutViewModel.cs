using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Model;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _platform = Environment.OSVersion.ToString();

    [ObservableProperty]
    private string _framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public AboutViewModel()
    {
        Platform = $"{PlatformHelper.GetPlatformName()} - {Environment.OSVersion}";
    }

    [RelayCommand]
    private void OpenGitHub() => PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom");

    [RelayCommand]
    private void OpenIssue() => PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/issues");

    [RelayCommand]
    private void OpenApiDoc() => PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/blob/master/LuaApi.md");
}
