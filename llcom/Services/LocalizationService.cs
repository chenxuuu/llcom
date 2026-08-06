using System;
using System.Windows;

namespace llcom.Tools;

/// <summary>
/// 本地化服务：负责切换界面语言资源字典（zh-CN / en-US 等）。
/// 从原 Tools/Global.cs 拆出（Step 2）。
/// </summary>
internal static class LocalizationService
{
    /// <summary>
    /// 更换语言文件
    /// </summary>
    /// <param name="languagefileName">语言文件名（不带 .xaml 后缀），如 "zh-CN"</param>
    public static void LoadLanguageFile(string languagefileName)
    {
        try
        {
            Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary()
            {
                Source = new Uri(
                    $"pack://application:,,,/languages/{languagefileName}.xaml",
                    UriKind.RelativeOrAbsolute
                ),
            };
        }
        catch
        {
            Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary()
            {
                Source = new Uri(
                    "pack://application:,,,/languages/en-US.xaml",
                    UriKind.RelativeOrAbsolute
                ),
            };
        }
    }
}
