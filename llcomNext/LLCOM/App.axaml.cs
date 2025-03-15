using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using LLCOM.ViewModels;
using LLCOM.Views;
using Microsoft.Extensions.DependencyInjection;

//可以让XAML文件中直接使用自定义的控件
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "LLCOM.Controls")]

namespace LLCOM;

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
        collection.AddSingleton<DataPageViewModel>();
        collection.AddSingleton<ToolsPageViewModel>();
        collection.AddSingleton<ScriptPageViewModel>();
        collection.AddSingleton<OnlinePageViewModel>();
        collection.AddSingleton<LogPageViewModel>();
        collection.AddSingleton<SettingPageViewModel>();
            
        collection.AddSingleton<PacketDataViewModel>();
        collection.AddSingleton<TerminalViewModel>();
        collection.AddSingleton<WaveformViewModel>();

        //用于获取别的ViewModel
        collection.AddSingleton<Func<Type, ViewModelBase>>(x => type => (ViewModelBase)serviceProvider!.GetService(type));
            
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