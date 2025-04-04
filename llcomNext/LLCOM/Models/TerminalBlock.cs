using System;
using Avalonia.Media;
using Wcwidth;

namespace LLCOM.Models;

/// <summary>
/// 一个终端块，包含各项信息
/// </summary>
public class TerminalBlock
{
    //颜色均只存储颜色值，30~37、90~97为颜色值，0为默认颜色
    
    public int Background { get; set; }
    public int Foreground { get; set; }
    
    public static string Color2BindingName(int color, bool isForeground)
    {
        if(color == 0)
            return isForeground ? "TerminalTheme.Foreground" : "TerminalTheme.Background";
        return "TerminalTheme.Code" + color.ToString();
    }
    public string ForegroundBindingName => Color2BindingName(Foreground, true);
    public string BackgroundBindingName => Color2BindingName(Background, false);

    
    public bool IsBold { get; set; }
    public bool IsUnderLine { get; set; }
    public bool IsItalic { get; set; }
    
    //文字数据里不应包含不可见字符，比如换行符、回车符等
    private string _text = String.Empty;
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            //计算长度
            _length = 0;
            foreach (var c in _text)
            { 
                _length += UnicodeCalculator.GetWidth(c);
            }
        }
    }

    private int _length = 0;

    public int Length => _length;
    

    /// <summary>
    /// 一个终端块，包含各项信息
    /// </summary>
    public TerminalBlock(string text,
        int background = 0,
        int foreground = 0,
        bool isBold = false,
        bool isUnderLine = false,
        bool isItalic = false)
    {
        Text = text;
        Background = background;
        Foreground = foreground;
        IsBold = isBold;
        IsUnderLine = isUnderLine;
        IsItalic = isItalic;
    }
}