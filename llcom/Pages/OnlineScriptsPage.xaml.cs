using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using llcom.Model;

namespace llcom.Pages;

/// <summary>
/// OnlineScriptsPage.xaml 的交互逻辑
/// </summary>
public partial class OnlineScriptsPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// 设置属性值，值变化时触发 PropertyChanged（XAML 生成的 g.cs 基类固定为 Page，
    /// 无法继承 ObservableObject/ObservablePage，故类内自带此帮助方法）
    /// </summary>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string propertyName = null
    )
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public OnlineScriptsPage()
    {
        InitializeComponent();
    }

    private int _progress = 0;
    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    private bool _isIndeterminate = true;
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetProperty(ref _isIndeterminate, value);
    }

    private string _loadingMsg = "";
    public string LoadingMsg
    {
        get => _loadingMsg;
        set => SetProperty(ref _loadingMsg, value);
    }

    private bool _isLoding = true;
    public bool IsLoding
    {
        get => _isLoding;
        set => SetProperty(ref _isLoding, value);
    }

    /// <summary>
    /// 加载中。。。
    /// </summary>
    /// <param name="show"></param>
    /// <param name="progress"></param>
    private void Loading(string show = null, int? progress = null)
    {
        LoadingMsg = show ?? TryFindResource("Loading") as string ?? "?!";
        IsIndeterminate = progress == null;
        Progress = progress ?? 0;
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
                var r = Tools.Global.GetOnlineScripts(
                    (got, total) =>
                    {
                        Loading(
                            TryFindResource("OnlineScriptLoading") as string,
                            (int)(got * 100.0 / total)
                        );
                    }
                );
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
    private bool _isInList = true;
    public bool IsInList
    {
        get => _isInList;
        set => SetProperty(ref _isInList, value);
    }

    private OnlineScript _scriptNow = new OnlineScript();
    public OnlineScript ScriptNow
    {
        get => _scriptNow;
        set => SetProperty(ref _scriptNow, value);
    }

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
                TryFindResource("OnlineScriptDownloadTitle") as string ?? "?!"
            );
            if (!result)
                return;
            //文件已经有了
            if (File.Exists($"{Tools.Global.ProfilePath}user_script_run/{fileName}.lua"))
            {
                Tools.MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                continue; //回到最开始
            }

            try
            {
                File.WriteAllText(
                    $"{Tools.Global.ProfilePath}user_script_run/{fileName}.lua",
                    ScriptNow.Script
                );
                Tools.Global.RefreshLuaScriptList();
                Tools.MessageBox.Show(TryFindResource("SaveSucceed") as string ?? "?!");
            }
            catch (Exception err)
            {
                Tools.MessageBox.Show(err.ToString());
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
