using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace llcom.Pages
{
    /// <summary>
    /// MqttTestPage.xaml 的交互逻辑
    /// </summary>
    public partial class MqttTestPage : Page
    {
        public MqttTestPage()
        {
            InitializeComponent();
        }

        private bool initial = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (initial)
                return;




            initial = true;
        }
    }
}
