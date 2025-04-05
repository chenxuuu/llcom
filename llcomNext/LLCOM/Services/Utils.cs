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

    public static Setting Setting = null!;
    
    /// <summary>
    /// 启动软件时的初始化操作
    /// </summary>
    public static void Initial()
    {
        //初始化编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        //初始化设置
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

    private static string? _updateUrl;
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

    /// <summary>
    /// 获取字体名称，会根据字体名称返回一个可用的字体名称
    /// 如果字体名称不存在，则返回默认字体名称
    /// </summary>
    /// <param name="name">字体名</param>
    /// <returns>可用的字体名</returns>
    public static string CheckFontName(string name) =>
        (SkiaSharp.SKFontManager.Default.MatchFamily(name) ?? SkiaSharp.SKTypeface.Default).FamilyName;

    /// <summary>
    /// 获取字体名称，会根据字体名称返回一个可用的字体名称
    /// 如果字体名称不存在，则返回默认字体名称
    /// </summary>
    /// <param name="name">字体名</param>
    /// <returns>可用的字体名</returns>
    public static string CheckMonoFontName(string name)
    {
        var fontMgr = SkiaSharp.SKFontManager.Default;
        var font = fontMgr.MatchFamily(name) ?? SkiaSharp.SKTypeface.Default;
        if (font.IsFixedPitch)//是等宽字体
            return font.FamilyName;

        //不同的操作系统下的等宽字体都试试
        string[] defaultFontList =
        [
            "Consolas",
            "Menlo",
            "Monaco",
            "DejaVu Sans Mono",
            "Liberation Mono",
            "Ubuntu Mono",
            "Source Code Pro",
            "Fira Code",
            "Courier New",
            "Courier",
        ];
        foreach (var defaultFont in defaultFontList)
        {
            font = fontMgr.MatchFamily(defaultFont);
            if (font == null)
                continue;
            if (font.IsFixedPitch)
                return font.FamilyName;
        }
        //如果没有找到等宽字体，返回第一个等宽字体
        foreach (var f in fontMgr.FontFamilies)
        {
            font = fontMgr.MatchFamily(f);
            if (font.IsFixedPitch)
                return font.FamilyName;
        }
        //如果还是没有找到，返回默认字体
        return SkiaSharp.SKTypeface.Default.FamilyName;
    }

    /// <summary>
    /// 计算终端窗口可以显示的行数和列数
    /// </summary>
    /// <param name="width">窗口宽度</param>
    /// <param name="height">窗口高度</param>
    /// <param name="font">字体名称</param>
    /// <param name="fontSize">字体大小</param>
    /// <returns>(列数宽度,行数高度)</returns>
    public static (int, int) CalculateSize(double width, double height, string font,double fontSize)
    {
        //根据当前字体大小和窗口大小，计算出可以显示的行数和列数
        //按实际的字符宽度和高度来计算
        var fontFamily = SkiaSharp.SKFontManager.Default.MatchFamily(font) ?? SkiaSharp.SKTypeface.Default;

        //计算出每个字符的宽度和高度，用skiasharp的接口来计算
        //主要字符之间会有间距，所以需要用实际的字符来计算
        var paint = new SkiaSharp.SKPaint
        {
            Typeface = fontFamily,
            TextSize = (float)fontSize
        };

        // 使用二分法计算出一行能放下多少个字符
        int columns = 1;
        int low = 1, high = 9999;
        char testChar = 'W'; // 测试字符
        while (low <= high)
        {
            int mid = (low + high) / 2;
            var realWidth = paint.MeasureText(new string(testChar, mid));
            if (realWidth > width)
            {
                high = mid - 1;
            }
            else
            {
                columns = mid;
                low = mid + 1;
            }
        }
            
        //计算出能放下多少行 TODO)) 待修正
        var charHeight = paint.FontSpacing;
        int rows = (int)(height / charHeight);
        
        //注意不要让行数和列数为0
        var minRows = 1;
        var minColumns = 20;
        if (rows < minRows)
            rows = minRows;
        if (columns < minColumns)
            columns = minColumns;
        
        //返回
        return (columns, rows);
    }
}