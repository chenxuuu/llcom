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
    [ObservableProperty]
    private FontFamily _packetFontFamily;
    
    //分包数据的字体名称
    private string _packetFont = Database.Get(nameof(PacketFont), "").Result;
    public string PacketFont => _packetFont;
    

    public Setting()
    {
        var font = SkiaSharp.SKFontManager.Default.MatchFamily(PacketFont);
        font ??= SkiaSharp.SKTypeface.Default;
        PacketFontFamily = font.FamilyName;
        if(string.IsNullOrEmpty(PacketFont))
            _packetFont = font.FamilyName;
        
        PropertyChanged += async (sender, e) =>
        {
            //某个变量被更改
            var name = e.PropertyName;
            
            //需要特殊处理的变量
            switch (name)
            {
                case nameof(PacketFontFamily):
                    _packetFont = PacketFontFamily.Name;
                    await Database.Set(nameof(PacketFont), PacketFont);
                    break;
            }
            
            //不需要存储的变量
            string[] dismissList = 
            [
                nameof(PacketFontFamily),
            ];
            if (name == null || dismissList.Contains(name))
                return;

            var value = GetType().GetProperty(name)?.GetValue(this);
            var valueString = value?.ToString() ?? "";
            await Database.Set(name, valueString);
        };
    }
}