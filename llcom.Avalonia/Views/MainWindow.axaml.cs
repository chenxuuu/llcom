using Avalonia.Controls;
using llcom.Avalonia.ViewModels;

namespace llcom.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Clean up serial port on close
        try
        {
            Tools.UartManager.Instance.Stop();
        }
        catch { }

        base.OnClosing(e);
    }
}
