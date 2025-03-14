using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace LLCOM.Services;

public class Utils
{
    private static string? _version = null;
    /// <summary>
    /// 软件版本
    /// </summary>
    public static string Version
    {
        get
        {
            if (_version is null)
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
                _version = $"{version.Major}.{version.Minor}.{version.Build}";
            }
            return _version;
        }
    }

    /// <summary>
    /// 用户配置文件路径
    /// win：C:\Users\{username}\LLCOM_Next
    /// linux：/home/{username}/LLCOM_Next
    /// mac：/Users/{username}/LLCOM_Next
    /// </summary>
    public readonly static string AppPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "LLCOM_Next");
    
    /// <summary>
    /// 启动软件时的初始化操作
    /// </summary>
    public static void Initial()
    {
        if (!Directory.Exists(AppPath))
            Directory.CreateDirectory(AppPath);
        //初始化语言 TODO
    }
    
    public static async Task<bool> CopyString(string txt)
    {
        try
        {
            TopLevel top;
            var app = Application.Current!.ApplicationLifetime;
            if (app is IClassicDesktopStyleApplicationLifetime desktop)
            {
                top = desktop.MainWindow!;
            }
            else if (app is ISingleViewApplicationLifetime single)
            {
                top = TopLevel.GetTopLevel(single.MainView)!;
            }
            else
                return false;
            var clipboard = top.Clipboard;
            await clipboard!.SetTextAsync(txt);
            return true;
        }
        catch
        {
            return false;
        }
    }
}