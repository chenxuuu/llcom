using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using FontAwesome.WPF;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
using System.Windows.Shapes;
using System.Xml;
using llcom.ViewModels;

namespace llcom
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
        }

        //发送/接收转换脚本编辑器（与主窗口 Lua 编辑器共用 LuaEditorViewModel）
        private LuaEditorViewModel _sendEditor;
        private LuaEditorViewModel _recvEditor;







        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = Tools.Global.setting;

            //重写关闭响应代码
            this.Closing += SettingWindow_Closing;

            //置顶显示以免被挡住
            this.Topmost = true;

            //初始化下拉框参数
            dataBitsComboBox.SelectedIndex = Tools.Global.setting.dataBits - 5;
            stopBitComboBox.SelectedIndex = Tools.Global.setting.stopBit - 1;
            dataCheckComboBox.SelectedIndex = Tools.Global.setting.parity;

            showHexComboBox.DataContext = Tools.Global.setting;
            //luaTestHexCheck.DataContext = Tools.Global.setting;
            //luaTestHexCheckRev.DataContext = Tools.Global.setting;

            //快速搜索
            SearchPanel.Install(textEditor.TextArea);
            SearchPanel.Install(textEditorRev.TextArea);
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream s = assembly.GetManifestResourceStream(name))
            {
                using (XmlTextReader reader = new XmlTextReader(s))
                {
                    var xshd = HighlightingLoader.LoadXshd(reader);
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                    textEditorRev.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }
            //加载上次打开的文件（LuaEditorViewModel 构造时已加载）
            _sendEditor = new LuaEditorViewModel(
                "user_script_send_convert/",
                () => Tools.Global.setting.sendScript,
                v => Tools.Global.setting.sendScript = v,
                () => LuaEnv.LuaLoader.ClearRun());
            _recvEditor = new LuaEditorViewModel(
                "user_script_recv_convert/",
                () => Tools.Global.setting.recvScript,
                v => Tools.Global.setting.recvScript = v,
                () => LuaEnv.LuaLoader.ClearRun(),
                f => Tools.Global.recvScriptBackup = f);
            luaFileList.DataContext = _sendEditor;
            luaFileListRev.DataContext = _recvEditor;
            //编辑器文本桥接（AvalonEdit Text 不可绑定，经回调访问）
            _sendEditor.SetTextBridge(() => textEditor.Text, t => textEditor.Text = t);
            _recvEditor.SetTextBridge(() => textEditorRev.Text, t => textEditorRev.Text = t);
            //加载编码
            var el = Encoding.GetEncodings();
            List<EncodingInfo> encodingList = new List<EncodingInfo>(el);
            //先排个序，美观点
            encodingList.Sort((x, y) => x.CodePage - y.CodePage);
            foreach (var en in encodingList)
            {
                ComboBoxItem c = new ComboBoxItem();
                c.Content = $"[{en.CodePage}] {en.Name}";
                c.Tag = en.CodePage;
                int index = encodingComboBox.Items.Add(c);
                if (Tools.Global.setting.encoding == en.CodePage)//现在用的编码
                    encodingComboBox.SelectedIndex = index;
            }
        }

        private void SettingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //自动保存脚本
            _sendEditor?.OnAutoSave();
            _recvEditor?.OnAutoSave();
            if (Tools.Global.isMainWindowsClosed)
            {
                //说明软件关了
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;//取消这次关闭事件
                Hide();//隐藏窗口，以便下次调用show
            }
        }

        private void ApiDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Tools.Global.apiDocumentUrl);
        }

        private void OpenScriptFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Tools.Global.GetTrueProfilePath() + "user_script_send_convert");
        }

        private void DataBitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(dataBitsComboBox.SelectedItem != null)
            {
                Tools.Global.setting.dataBits = dataBitsComboBox.SelectedIndex + 5;
            }
        }

        private void StopBitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (stopBitComboBox.SelectedItem != null)
            {
                Tools.Global.setting.stopBit = stopBitComboBox.SelectedIndex + 1;
            }
        }

        private void DataCheckComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataCheckComboBox.SelectedItem != null)
            {
                Tools.Global.setting.parity = dataCheckComboBox.SelectedIndex;
                //Tools.MessageBox.Show((dataCheckComboBox.SelectedItem as ComboBoxItem).Content.ToString());
            }
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanel.Visibility = Visibility.Collapsed;
            newLuaFileWrapPanel.Visibility = Visibility.Visible;
        }

        private void NewLuaFilebutton_Click(object sender, RoutedEventArgs e)
        {
            //创建/重名校验与加载都在 VM.CreateNew 内完成
            _sendEditor.CreateNew(newLuaFileNameTextBox.Text);
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void NewLuaFileCancelbutton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void TestScriptButton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
            luaTestWrapPanel.Visibility = Visibility.Visible;
        }







        private void LuaTestbutton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_sendEditor?.SelectedFile))
            {
                try
                {
                    byte[] r = LuaEnv.LuaLoader.Run($"{_sendEditor.SelectedFile}.lua",
                                        new System.Collections.ArrayList{"uartData",
                                            (bool)luaTestHexCheck.IsChecked ? Tools.Global.Hex2Byte(luaTestTextBox.Text) :
                                            Tools.Global.GetEncoding().GetBytes(luaTestTextBox.Text)});
                    Tools.MessageBox.Show($"{TryFindResource("SettingLuaRunResult") as string ?? "?!"}\r\nHEX：" + Tools.Global.Byte2Hex(r) +
                        $"\r\n{TryFindResource("SettingLuaRawText") as string ?? "?!"}" + Tools.Global.Byte2Readable(r));
                }
                catch(Exception ex)
                {
                    Tools.MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                }

            }
        }

        private void LuaTestCancelbutton_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            //自动保存脚本
            _sendEditor?.OnAutoSave();
        }

        private void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Tools.Global.GetTrueProfilePath() + "logs");
            }
            catch
            {
                Tools.MessageBox.Show($"尝试打开文件夹失败，请自行打开该路径：{Tools.Global.GetTrueProfilePath()}logs");
            }
        }

        private void encodingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox c = sender as ComboBox;
            if ((int)((ComboBoxItem)c.SelectedItem).Tag == Tools.Global.setting.encoding)
                return;
            Tools.Global.setting.encoding = (int)((ComboBoxItem)c.SelectedItem).Tag;
        }



        private void newScriptButtonRev_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanelRev.Visibility = Visibility.Collapsed;
            newLuaFileWrapPanelRev.Visibility = Visibility.Visible;
        }

        private void newLuaFilebuttonRev_Click(object sender, RoutedEventArgs e)
        {
            //创建/重名校验与加载都在 VM.CreateNew 内完成
            _recvEditor.CreateNew(newLuaFileNameTextBoxRev.Text);
            newLuaFileWrapPanelRev.Visibility = Visibility.Collapsed;
        }

        private void newLuaFileCancelbuttonRev_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanelRev.Visibility = Visibility.Collapsed;
        }

        private void testScriptButtonRev_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanelRev.Visibility = Visibility.Collapsed;
            luaTestWrapPanelRev.Visibility = Visibility.Visible;
        }

        private void openScriptFolderButtonRev_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Tools.Global.GetTrueProfilePath() + "user_script_recv_convert");
        }





        private void luaTestbuttonRev_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_recvEditor?.SelectedFile))
            {
                try
                {
                    byte[] r = LuaEnv.LuaLoader.Run(
                        $"{_recvEditor.SelectedFile}.lua",
                        new System.Collections.ArrayList{
                            "uartData", (bool)(luaTestHexCheckRev.IsChecked) ? 
                            Tools.Global.Hex2Byte(luaTestTextBoxRev.Text) : 
                            Tools.Global.GetEncoding().GetBytes(luaTestTextBoxRev.Text),
                        },
                        "user_script_recv_convert/");
                    Tools.MessageBox.Show($"{TryFindResource("SettingLuaRunResult") as string ?? "?!"}\r\nHEX：" + Tools.Global.Byte2Hex(r) +
                        $"\r\n{TryFindResource("SettingLuaRawText") as string ?? "?!"}" + Tools.Global.Byte2Readable(r));
                }
                catch (Exception ex)
                {
                    Tools.MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                }
            }
        }

        private void luaTestCancelbuttonRev_Click(object sender, RoutedEventArgs e)
        {
            luaTestWrapPanelRev.Visibility = Visibility.Collapsed;
        }

        private void textEditorRev_LostFocus(object sender, RoutedEventArgs e)
        {
            //自动保存脚本
            _recvEditor?.OnAutoSave();
        }
    }
}
