using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RestSharp;

namespace LLCOM.Services;

public static class Utils
{
    private static string? _version;
    
    /// <summary>
    /// 软件版本
    /// </summary>
    public static string Version => _version ??= System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    private static string? _appPath;
    /// <summary>
    /// 用户配置文件路径
    /// win：C:\Users\{username}\.LLCOM_Next
    /// linux：/home/{username}/.LLCOM_Next
    /// mac：/Users/{username}/.LLCOM_Next
    /// </summary>
    public static string AppPath => _appPath ??= InitializeAppPath();
    private static string InitializeAppPath()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appPath = Path.Combine(path, ".LLCOM_Next");
        if (!Directory.Exists(appPath))
            Directory.CreateDirectory(appPath);
        return appPath;
    }

    public static Setting Setting { get; set; }
    
    /// <summary>
    /// 启动软件时的初始化操作
    /// </summary>
    public static void Initial()
    {
        //初始化编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Setting = new Setting();
        //初始化语言 TODO
    }
    
    /// <summary>
    /// 复制文本到剪贴板
    /// </summary>
    /// <param name="txt">文本数据</param>
    /// <returns>是否复制成功</returns>
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
    
    /// <summary>
    /// 打开网页链接
    /// </summary>
    /// <param name="url">网址</param>
    public static void OpenWebLink(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // ignored
        }
    }

    private static string? _updateUrl = null;
    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns>是否有新版本，有的话返回下载地址</returns>
    public static async Task<string?> CheckUpdate()
    {
        if(_updateUrl != null)
            return _updateUrl;
        
        var versionNow = Services.Utils.Version;
        //获取https://api.github.com/repos/chenxuuu/llcom/releases/latest的json结果
        var client = new RestClient("https://api.github.com/repos/chenxuuu/llcom/releases/latest");
        var request = new RestRequest();
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            return null;
        
        var json = System.Text.Json.JsonDocument.Parse(response.Content);
        var version = json.RootElement.GetProperty("tag_name").GetString();
        if(version == null)
            return null;
        
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

        return _updateUrl;
    }
    
    public static bool HasUpdate() => _updateUrl != null;
}