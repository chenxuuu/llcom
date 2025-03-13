using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LLCOM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private Func<Type, ViewModelBase?> GetService;
    

    
    public MainViewModel(Func<Type, ViewModelBase?> getService)
    {
        GetService = getService;
    }

}
