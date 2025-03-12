using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LLCOM.ViewModels;
using LLCOM.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LLCOM
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var collection = new ServiceCollection();
            ServiceProvider? serviceProvider = null;
            
            collection.AddSingleton<MainWindowViewModel>();
            collection.AddSingleton<MainViewModel>();

            //用于获取别的ViewModel
            collection.AddSingleton<Func<Type, ViewModelBase?>>(x => type => serviceProvider!.GetService(type) as ViewModelBase);
            
            serviceProvider = collection.BuildServiceProvider();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                desktop.MainWindow = new MainWindow
                {
                    DataContext = serviceProvider.GetService<MainWindowViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}