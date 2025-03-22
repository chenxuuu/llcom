using System;

namespace LLCOM.ViewModels;

public partial class OnlinePageViewModel: ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public OnlinePageViewModel() {}
    
    public OnlinePageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
    }
}