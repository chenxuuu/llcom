using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ursa.Controls;

namespace LLCOM.ViewModels;

public partial class DataPageViewModel: ViewModelBase
{
    private Func<Type, ViewModelBase> _getService;
    
    [ObservableProperty]
    private int _selectedTabIndex = 0;
    
    [ObservableProperty]
    private ObservableCollection<DataPageTabItemModel> _tabList = new();
    
    //用于设计时预览，正式代码中无效
    public DataPageViewModel() {}
    
    public DataPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
        
        TabList.Add(new DataPageTabItemModel("(1)串口", _getService(typeof(LogPageViewModel))));
        TabList.Add(new DataPageTabItemModel("(2)TCP服务端", _getService(typeof(OnlinePageViewModel))));
    }
    
    [RelayCommand]
    private void AddTabItem()
    {
        var name = $"({TabList.Count + 1})测试xx";
        TabList.Add(new DataPageTabItemModel(name, _getService(typeof(OnlinePageViewModel))));
    }

    [RelayCommand]
    private async Task RemoveTabItem(DataPageTabItemModel model)
    {
        var r = await MessageBox.ShowAsync("确定要删除这个测试卡吗？", "删除提示",
            icon: MessageBoxIcon.Warning, button: MessageBoxButton.YesNo);
        if(r == MessageBoxResult.Yes)
            TabList.Remove(model);
    }
}

public class DataPageTabItemModel(string header, ViewModelBase content)
{
    public string Header { get; } = header;
    public ViewModelBase Content { get; } = content;
}