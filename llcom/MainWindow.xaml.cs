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

namespace llcom
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<ToSendData> items = new List<ToSendData>();
            items.Add(new ToSendData() { id = 1, text = "AT", hex = false });
            items.Add(new ToSendData() { id = 2, text = "ATI", hex = false });
            items.Add(new ToSendData() { id = 3, text = "AT+CREG?", hex = false });
            items.Add(new ToSendData() { id = 4, text = "AT+CGATT?", hex = false });
            items.Add(new ToSendData() { id = 5, text = "AT+CIPSEND=2,0", hex = false });
            items.Add(new ToSendData() { id = 6, text = "AA BB CC DD", hex = true });
            items.Add(new ToSendData() { id = 7, text = "11 22 66 22 44", hex = true });

            toSendList.ItemsSource = items;
        }
    }

    public class ToSendData
    {
        public int id { get; set; }
        public string text { get; set; }
        public bool hex { get; set; }
    }
}
