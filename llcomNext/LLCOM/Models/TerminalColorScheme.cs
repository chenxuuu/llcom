using Avalonia.Media;

namespace LLCOM.Models;

public class TerminalColorScheme(string name)
{
    public string Name { get; set; } = name;
    
    public required IBrush Foreground { get; set; }
    public required IBrush Background { get; set; }
    public required IBrush Cursor { get; set; }
    public required IBrush SelectionBackground { get; set; }
    
    //black
    public required IBrush Code40 { get; set; }
    //red
    public required IBrush Code41 { get; set; }
    //green
    public required IBrush Code42 { get; set; }
    //yellow
    public required IBrush Code43 { get; set; }
    //blue
    public required IBrush Code44 { get; set; }
    //purple
    public required IBrush Code45 { get; set; }
    //cyan
    public required IBrush Code46 { get; set; }
    //white
    public required IBrush Code47 { get; set; }
    
    //bright black
    public required IBrush Code30 { get; set; }
    //bright red
    public required IBrush Code31 { get; set; }
    //bright green
    public required IBrush Code32 { get; set; }
    //bright yellow
    public required IBrush Code33 { get; set; }
    //bright blue
    public required IBrush Code34 { get; set; }
    //bright purple
    public required IBrush Code35 { get; set; }
    //bright cyan
    public required IBrush Code36 { get; set; }
    //bright white
    public required IBrush Code37 { get; set; }
}