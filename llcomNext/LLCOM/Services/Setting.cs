using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LLCOM.Services;

public partial class Setting : ObservableObject
{
    private static readonly Database Database = new("setting.db");
    
    //是否折叠分包数据的标签
    //全显示、折叠头、纯文本
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowHeader),nameof(PureText))]
    private bool? _isPacketShowHeader = Database.Get(nameof(IsPacketShowHeader), true).Result;
    
    //是否显示分包头
    public bool ShowHeader => IsPacketShowHeader is null;
    //是否纯文本
    public bool PureText => IsPacketShowHeader is false;

    //是否转义不可见字符
    [ObservableProperty]
    private bool _isEscapeInvisibleChar = Database.Get(nameof(IsEscapeInvisibleChar), true).Result;
    
    //Hex显示模式
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowString),nameof(IsShowHexSmall))]
    private bool? _isHexMode = Database.Get(nameof(IsHexMode), (bool?)null).Result;
    
    //是否显示字符串，给Hex显示模式使用，不存储
    public bool IsShowString => IsHexMode is null || !IsHexMode.Value;
    //是否显示hex小字，给Hex显示模式使用，不存储
    public bool IsShowHexSmall => IsHexMode is null;
    
    //分包数据的字体
    ///////////数据包的字体
    public FontFamily? PacketFontFamily
    {
        get
        {
            if(string.IsNullOrEmpty(PacketFont))
                PacketFont = Utils.CheckFontName(PacketFont);
            return PacketFont;
        }
        set
        {
            if (value is null)
                return;
            PacketFont = value.Name;
            OnPropertyChanged();
        }
    }
    //字体名称
    [ObservableProperty]
    private string _packetFont = Database.Get(nameof(PacketFont), "").Result;
    //////////Hex小字的字体
    public FontFamily? PacketHexFontFamily
    {
        get
        {
            if(string.IsNullOrEmpty(PacketHexFont))
                PacketHexFont = Utils.CheckFontName(PacketHexFont);
            return PacketHexFont;
        }
        set
        {
            if (value is null)
                return;
            PacketHexFont = value.Name;
            OnPropertyChanged();
        }
    }
    //字体名称
    [ObservableProperty]
    private string _packetHexFont = Database.Get(nameof(PacketHexFont), "").Result;
    //////////标题头的字体
    public FontFamily? PacketHeaderFontFamily
    {
        get
        {
            if(string.IsNullOrEmpty(PacketHeaderFont))
                PacketHeaderFont = Utils.CheckFontName(PacketHeaderFont);
            return PacketHeaderFont;
        }
        set
        {
            if (value is null)
                return;
            PacketHeaderFont = value.Name;
            OnPropertyChanged();
        }
    }
    //字体名称
    [ObservableProperty]
    private string _packetHeaderFont = Database.Get(nameof(PacketHeaderFont), "").Result;
    //////////时间标记的字体
    public FontFamily? PacketExtraFontFamily
    {
        get
        {
            if(string.IsNullOrEmpty(PacketExtraFont))
                PacketExtraFont = Utils.CheckFontName(PacketExtraFont);
            return PacketExtraFont;
        }
        set
        {
            if (value is null)
                return;
            PacketExtraFont = value.Name;
            OnPropertyChanged();
        }
    }
    //字体名称
    [ObservableProperty]
    private string _packetExtraFont = Database.Get(nameof(PacketExtraFont), "").Result;
    ///////////终端模式下的字体
    public FontFamily? TerminalFontFamily
    {
        get
        {
            if(string.IsNullOrEmpty(TerminalFont))
                TerminalFont = Utils.CheckFontName(TerminalFont);
            return TerminalFont;
        }
        set
        {
            if (value is null)
                return;
            TerminalFont = value.Name;
            OnPropertyChanged();
        }
    }
    //字体名称
    [ObservableProperty]
    private string _terminalFont = Database.Get(nameof(TerminalFont), "").Result;
    
    //终端模式的主题
    [ObservableProperty]
    private int _terminalTheme = Database.Get(nameof(TerminalTheme), 0).Result;
    
    
    
    public Setting()
    {
        PropertyChanged += async (sender, e) =>
        {
            //某个变量被更改
            var name = e.PropertyName;
            
            //不需要存储的变量
            string[] dismissList = 
            [
                nameof(PacketFontFamily),
                nameof(PacketHexFontFamily),
                nameof(PacketHeaderFontFamily),
                nameof(PacketExtraFontFamily),
                nameof(TerminalFontFamily),
            ];
            if (name == null || dismissList.Contains(name))
                return;

            var value = GetType().GetProperty(name)?.GetValue(this);
            var valueString = value?.ToString() ?? "";
            await Database.Set(name, valueString);
        };
    }
}