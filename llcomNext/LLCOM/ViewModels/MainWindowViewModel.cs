using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LLCOM.ViewModels
{

    public partial class MainWindowViewModel(Func<Type, ViewModelBase?> getService) : ViewModelBase
    {
        [ObservableProperty]
        private MainViewModel _mainViewModel = (MainViewModel)getService(typeof(MainViewModel))!;
    }
}