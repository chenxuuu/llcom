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
    /// EncodingFixPage.xaml 的交互逻辑
    /// </summary>
    public partial class EncodingFixPage : Page
    {
        public EncodingFixPage()
        {
            InitializeComponent();
        }
        class fixedData
        {
            public string raw { get; set; }
            public string target { get; set; }
            public string result { get; set; }
        }

        string[] encodingList = new string[]
        {
            "UTF-8",
            "GBK",
            "windows-1252",
            "Big5",
            "Shift_Jis",
            "iso-8859-1",
        };

        private void RawTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FixResultList.Items.Clear();
            for(int i = 0; i < encodingList.Length; i++)
            {
                for(int j = 0; j < encodingList.Length; j++)
                {
                    if (i == j)
                        continue;
                    FixResultList.Items.Add(new fixedData 
                    { 
                        raw = encodingList[i], 
                        target = encodingList[j], 
                        result = Encoding.GetEncoding(encodingList[i]).GetString(Encoding.GetEncoding(encodingList[j]).GetBytes(RawTextBox.Text))
                    });
                }
            }
        }

        private void FixResultList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                //获取单元格内容
                string copiedData = (FixResultList.SelectedItem as fixedData).result;
                if (string.IsNullOrEmpty(copiedData)) return;
                //复制到剪贴板
                Clipboard.Clear();
                Clipboard.SetData(DataFormats.Text, copiedData);
                MessageBox.Show("copyed:\r\n" + copiedData);
            }
        }
    }
}
