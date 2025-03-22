using System;

namespace LLCOM.ViewModels;

public partial class ScriptPageViewModel : ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public ScriptPageViewModel() {}
    
    public ScriptPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
    }
}