using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace LLCOM.ViewModels
{

    public partial class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// 标题栏
        /// </summary>
        [ObservableProperty]
        public string _title = "LLCOM - Next";

        public MainWindowViewModel()
        {

        }
    }
}