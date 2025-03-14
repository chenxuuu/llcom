using System;

namespace LLCOM.ViewModels;

public partial class SettingPageViewModel : ViewModelBase
{
    private Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public SettingPageViewModel() {}
    
    public SettingPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
    }
}