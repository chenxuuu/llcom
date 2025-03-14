using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LLCOM.ViewModels;

public partial class DataPageViewModel: ViewModelBase
{
    private Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public DataPageViewModel() {}
    
    public DataPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
    }
}