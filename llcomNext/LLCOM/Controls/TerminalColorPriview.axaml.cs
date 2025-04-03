using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using LLCOM.Models;

namespace LLCOM.Controls;

public class TerminalColorPriview : TemplatedControl
{
    //主题色
    public static readonly StyledProperty<TerminalColorScheme?> MainColorProperty = 
        AvaloniaProperty.Register<PacketDataControl, TerminalColorScheme?>(nameof(MainColor));
    public TerminalColorScheme? MainColor
    {
        get => GetValue(MainColorProperty);
        set => SetValue(MainColorProperty, value);
    }
}