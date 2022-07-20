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
    /// SocketClientPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class SocketClientPage : Page
    {
        public SocketClientPage()
        {
            InitializeComponent();
        }
        private bool initial = false;

        public bool IsConnected { get; set; } = false;
        //是否可更改服务器信息
        public bool Changeable { get; set; } = true;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (initial)
                return;
            initial = true;

            this.DataContext = this;

            ServerTextBox.DataContext = Tools.Global.setting;
            PortTextBox.DataContext = Tools.Global.setting;
            ProtocolTypeComboBox.DataContext = Tools.Global.setting;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
