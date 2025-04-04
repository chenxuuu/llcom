using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LLCOM.Models;

public partial class TerminalColorScheme(string name) : ObservableObject
{
    public void RefreshData(TerminalColorScheme colorScheme)
    {
        this.Name = colorScheme.Name;
        this.Foreground = colorScheme.Foreground;
        this.Background = colorScheme.Background;
        this.Cursor = colorScheme.Cursor;
        this.SelectionBackground = colorScheme.SelectionBackground;
        this.Code30 = colorScheme.Code30;
        this.Code31 = colorScheme.Code31;
        this.Code32 = colorScheme.Code32;
        this.Code33 = colorScheme.Code33;
        this.Code34 = colorScheme.Code34;
        this.Code35 = colorScheme.Code35;
        this.Code36 = colorScheme.Code36;
        this.Code37 = colorScheme.Code37;
        this.Code90 = colorScheme.Code90;
        this.Code91 = colorScheme.Code91;
        this.Code92 = colorScheme.Code92;
        this.Code93 = colorScheme.Code93;
        this.Code94 = colorScheme.Code94;
        this.Code95 = colorScheme.Code95;
        this.Code96 = colorScheme.Code96;
        this.Code97 = colorScheme.Code97;
    }
    
    [ObservableProperty]
    private string _name = name;
    
    [ObservableProperty]
    private IBrush _foreground;
    
    [ObservableProperty]
    private IBrush _background;
    
    [ObservableProperty]
    private IBrush _cursor;
    
    [ObservableProperty]
    private IBrush _selectionBackground;
    
    //black
    [ObservableProperty]
    private IBrush _code30;
    
    //red
    [ObservableProperty]
    private IBrush _code31;
    
    //green
    [ObservableProperty]
    private IBrush _code32;
    
    //yellow
    [ObservableProperty]
    private IBrush _code33;
    
    //blue
    [ObservableProperty]
    private IBrush _code34;
    
    //purple
    [ObservableProperty]
    private IBrush _code35;
    
    //cyan
    [ObservableProperty]
    private IBrush _code36;
    
    //white
    [ObservableProperty]
    private IBrush _code37;
    
    //bright black
    [ObservableProperty]
    private IBrush _code90;
    
    //bright red
    [ObservableProperty]
    private IBrush _code91;
    
    //bright green
    [ObservableProperty]
    private IBrush _code92;
    
    //bright yellow
    [ObservableProperty]
    private IBrush _code93;
    
    //bright blue
    [ObservableProperty]
    private IBrush _code94;
    
    //bright purple
    [ObservableProperty]
    private IBrush _code95;
    
    //bright cyan
    [ObservableProperty]
    private IBrush _code96;
    
    //bright white
    [ObservableProperty]
    private IBrush _code97;
}