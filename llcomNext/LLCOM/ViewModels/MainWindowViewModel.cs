using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LLCOM.ViewModels
{

    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private MainViewModel _mainViewModel;

        [ObservableProperty]
        private string _title = "LLCOM";

        public MainWindowViewModel(Func<Type, ViewModelBase> getService)
        {
            _mainViewModel = (MainViewModel)getService(typeof(MainViewModel));
            Title += $" - {Services.Utils.Version}";
        }
    }
}