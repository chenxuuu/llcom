using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Xml;

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
        ObservableCollection<ToSendData> items = new ObservableCollection<ToSendData>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //初始化所有数据
            Tools.Global.Initial();

            this.Closing += MainWindow_Closing;

            

            toSendList.ItemsSource = items;

            items.Add(new ToSendData() { id = 1, text = "AT", hex = false });
            items.Add(new ToSendData() { id = 2, text = "ATI", hex = false });
            items.Add(new ToSendData() { id = 3, text = "AT+CREG?", hex = false });
            items.Add(new ToSendData() { id = 4, text = "AT+CGATT?", hex = false });
            items.Add(new ToSendData() { id = 5, text = "AT+CIPSEND=2,0", hex = false });
            items.Add(new ToSendData() { id = 6, text = "AA BB CC DD", hex = true });
            items.Add(new ToSendData() { id = 7, text = "11 22 66 22 44", hex = true });

            

            //快速搜索
            textEditor.TextArea.DefaultInputHandler.NestedInputHandlers.Add(
                new SearchInputHandler(textEditor.TextArea));
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream s = assembly.GetManifestResourceStream(name))
            {
                using (XmlTextReader reader = new XmlTextReader(s))
                {
                    var xshd = HighlightingLoader.LoadXshd(reader);
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Tools.Global.isMainWindowsClosed = true;
            foreach (Window win in App.Current.Windows)
            {
                if (win != this)
                {
                    win.Close();
                }
            }
            e.Cancel = false;//正常关闭
        }


        Window settingPage = new SettingWindow();
        private void MoreSettingButton_Click(object sender, RoutedEventArgs e)
        {
            settingPage.Show();
        }


        private void ApiDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/chenxuuu/llcom");
        }

        private void OpenScriptFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "user_script_run");
        }
    }

    public class ToSendData
    {
        public int id { get; set; }
        public string text { get; set; }
        public bool hex { get; set; }

    }
}
