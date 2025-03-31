using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
    

    public Setting()
    {
        PropertyChanged += async (sender, e) =>
        {
            string[] dismissList = 
            [
                //nameof(IsShowString),
            ];
            //某个变量被更改
            var name = e.PropertyName;
            if (name == null || dismissList.Contains(name))
                return;

            var value = GetType().GetProperty(name)?.GetValue(this);
            var valueString = value?.ToString() ?? "";
            await Database.Set(name, valueString);
        };
    }
}