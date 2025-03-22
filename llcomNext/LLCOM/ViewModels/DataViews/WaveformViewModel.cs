using System;

namespace LLCOM.ViewModels;

public partial class WaveformViewModel : ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public WaveformViewModel() {}
    
    public WaveformViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;
    }
}