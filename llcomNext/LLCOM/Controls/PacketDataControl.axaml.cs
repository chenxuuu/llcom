using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LLCOM.Controls;

public class PacketDataControl : TemplatedControl
{
    //主题色
    public static readonly StyledProperty<IBrush?> MainColorProperty = 
        AvaloniaProperty.Register<PacketDataControl, IBrush?>(nameof(MainColor));
    public IBrush? MainColor
    {
        get => GetValue(MainColorProperty);
        set => SetValue(MainColorProperty, value);
    }
    
    public static readonly StyledProperty<string?> IconProperty = 
        AvaloniaProperty.Register<PacketDataControl, string?>(nameof(Icon));
    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public static readonly StyledProperty<string?> HeaderProperty = 
        AvaloniaProperty.Register<PacketDataControl, string?>(nameof(Header));
    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    public static readonly StyledProperty<string?> ExtraProperty = 
        AvaloniaProperty.Register<PacketDataControl, string?>(nameof(Extra));
    public string? Extra
    {
        get => GetValue(ExtraProperty);
        set => SetValue(ExtraProperty, value);
    }
    
    
    public static readonly StyledProperty<string?> TextProperty = 
        AvaloniaProperty.Register<PacketDataControl, string?>(nameof(Text));
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<string?> HexProperty = 
        AvaloniaProperty.Register<PacketDataControl, string?>(nameof(Hex));
    public string? Hex
    {
        get => GetValue(HexProperty);
        set => SetValue(HexProperty, value);
    }
    
    public static readonly StyledProperty<bool?> ShowStringProperty = 
        AvaloniaProperty.Register<PacketDataControl, bool?>(nameof(ShowString), defaultValue: true);
    public bool? ShowString
    {
        get => GetValue(ShowStringProperty);
        set => SetValue(ShowStringProperty, value);
    }
    
    public static readonly StyledProperty<bool?> ShowHexProperty = 
        AvaloniaProperty.Register<PacketDataControl, bool?>(nameof(ShowHex), defaultValue: true);
    public bool? ShowHex
    {
        get => GetValue(ShowHexProperty);
        set => SetValue(ShowHexProperty, value);
    }
}