using System;
using Avalonia.Controls;
using llcom.Avalonia.ViewModels;

namespace llcom.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Cleanup();
        }
    }
}
