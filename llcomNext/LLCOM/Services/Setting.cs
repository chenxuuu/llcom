using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LLCOM.Services;

public partial class Setting : ObservableObject
{
    private static Database Database = new("setting.db");
    
    //是否折叠分包数据的标签
    [ObservableProperty]
    private bool _isPacketShowHeader = Database.Get(nameof(IsPacketShowHeader), true).Result;
    partial void OnIsPacketShowHeaderChanged(bool value) => Database.Set(nameof(IsPacketShowHeader), value).WaitAsync(TimeSpan.FromSeconds(2));

    //是否转义不可见字符
    [ObservableProperty]
    private bool _isEscapeInvisibleChar = Database.Get(nameof(IsEscapeInvisibleChar), true).Result;
    partial void OnIsEscapeInvisibleCharChanged(bool value) => Database.Set(nameof(IsEscapeInvisibleChar), value).WaitAsync(TimeSpan.FromSeconds(2));
    
    //Hex显示模式
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowString),nameof(IsShowHexSmall))]
    private bool? _isHexMode = Database.Get(nameof(IsHexMode), (bool?)null).Result;
    partial void OnIsHexModeChanged(bool? value) => Database.Set(nameof(IsHexMode), value).WaitAsync(TimeSpan.FromSeconds(2));
    
    //是否显示字符串，给Hex显示模式使用，不存储
    public bool IsShowString => IsHexMode is null || !IsHexMode.Value;
    //是否显示hex小字，给Hex显示模式使用，不存储
    public bool IsShowHexSmall => IsHexMode is null;
}