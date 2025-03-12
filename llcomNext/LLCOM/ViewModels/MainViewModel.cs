using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLCOM.ViewModels;

public partial class MainViewModel(Func<Type, ViewModelBase?> getService) : ViewModelBase
{

}
