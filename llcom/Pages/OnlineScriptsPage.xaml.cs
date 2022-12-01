using llcom.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace llcom.Pages
{
    /// <summary>
    /// OnlineScriptsPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class OnlineScriptsPage : Page
    {
        public OnlineScriptsPage()
        {
            InitializeComponent();
        }

        public int Progress { get; set; } = 0;
        public bool IsIndeterminate { get; set; } = true;
        public string LoadingMsg { get; set; } = "";
        public bool IsLoding { get; set; } = true;
        /// <summary>
        /// 加载中。。。
        /// </summary>
        /// <param name="show"></param>
        /// <param name="progress"></param>
        private void Loading(string show = null, int? progress = null)
        {
            LoadingMsg = show ?? TryFindResource("Loading") as string ?? "?!";
            IsIndeterminate = progress == null;
            Progress = progress  ?? 0;
            IsLoding = true;
        }
        private void UnLoading() => IsLoding = false;


        ObservableCollection<OnlineScript> scripts = new ObservableCollection<OnlineScript>();
        /// <summary>
        /// 刷新脚本列表
        /// </summary>
        /// <returns></returns>
        private async Task RefreshList()
        {
            Loading(TryFindResource("OnlineScriptLoading") as string);
            scripts.Clear();
            await Task.Run(() =>
            {
                try
                {
                    var r = Tools.Global.GetOnlineScripts((got, total) =>
                    {
                        Loading(TryFindResource("OnlineScriptLoading") as string, (int)(got * 100.0 / total));
                    });
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var d in r)
                            scripts.Add(d);
                    });
                }
                catch { }
            });
            UnLoading();
        }

        private static bool loaded = false;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;

            this.DataContext = this;

            //绑上去
            ScriptListItemsControl.ItemsSource = scripts;

            //打开时刷新一下
            await RefreshList();
        }
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshList();
        }
        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/chenxuuu/llcom/discussions/87");
            }
            catch { }
        }

        /// <summary>
        /// 是否在看脚本列表页？
        /// </summary>
        public bool IsInList { get; set; } = true;

        public OnlineScript ScriptNow { get; set; } = new OnlineScript();

        //打开了某个脚本的详情页
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var data = ((Button)sender).Tag as OnlineScript;
            if (data == null)
                return;
            ScriptNow.Author = data.Author;
            ScriptNow.Version = data.Version;
            ScriptNow.Name = data.Name;
            ScriptNow.Description = data.Description;
            ScriptNow.Note = data.Note;
            ScriptNow.Url = data.Url;
            ScriptNow.Script = data.Script;

            IsInList = false;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            IsInList = true;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                var (result, fileName) = Tools.InputDialog.OpenDialog(
                        TryFindResource("OnlineScriptDownloadSaveNotice") as string ?? "?!",
                        $"{ScriptNow.Name}",
                        TryFindResource("OnlineScriptDownloadTitle") as string ?? "?!");
                if (!result)
                    return;
                //文件已经有了
                if (File.Exists($"{Tools.Global.ProfilePath}user_script_run/{fileName}.lua"))
                {
                    MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                    continue;//回到最开始
                }

                try
                {
                    File.WriteAllText($"{Tools.Global.ProfilePath}user_script_run/{fileName}.lua", ScriptNow.Script);
                    Tools.Global.RefreshLuaScriptList();
                    MessageBox.Show(TryFindResource("SaveSucceed") as string ?? "?!");
                }
                catch(Exception err)
                {
                    MessageBox.Show(err.ToString());
                }
                return;
            }

        }

        private void UrlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(ScriptNow.Url);
            }
            catch { }
        }
    }
}
