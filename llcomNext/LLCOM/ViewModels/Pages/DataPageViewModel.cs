using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

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
        
        _tabList.Add(new DataPageTabItemModel("test1", _getService(typeof(LogPageViewModel))));
        _tabList.Add(new DataPageTabItemModel("test2", _getService(typeof(OnlinePageViewModel))));
    }
}

public class DataPageTabItemModel(string header, ViewModelBase content)
{
    public string Header { get; } = header;
    public ViewModelBase Content { get; } = content;
}