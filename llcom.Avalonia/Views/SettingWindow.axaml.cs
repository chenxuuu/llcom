using AvWindow = global::Avalonia.Controls.Window;
using RoutedEventArgs = global::Avalonia.Interactivity.RoutedEventArgs;

namespace llcom.Avalonia.Views;

public partial class SettingWindow : AvWindow
{
    public SettingWindow()
    {
        InitializeComponent();
    }

    public void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
