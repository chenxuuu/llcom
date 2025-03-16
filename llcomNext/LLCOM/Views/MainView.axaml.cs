using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LLCOM.ViewModels;

namespace LLCOM.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        await ((MainViewModel)this.DataContext!).PageLoadedCommand.ExecuteAsync(null);
    }
}