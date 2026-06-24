using System.Collections.Generic;
using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _assemblyVersion = "";

    [ObservableProperty]
    private string _platform = Environment.OSVersion.ToString();

    [ObservableProperty]
    private string _framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public List<string> ThanksProjects { get; } = new()
    {
        "MoonSharp (Lua)",
        "Avalonia UI",
        "AvaloniaEdit",
        "LibUsbDotNet",
        "CommunityToolkit.Mvvm",
        "SkiaSharp"
    };

    public AboutViewModel()
    {
        Platform = $"{PlatformHelper.GetPlatformName()} - {Environment.OSVersion}";
        AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    [RelayCommand]
    private void OpenGitHub() => PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom");

    [RelayCommand]
    private void OpenIssue() => PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/issues");

    [RelayCommand]
    private void OpenApiDoc() => PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/blob/master/LuaApi.md");
}
