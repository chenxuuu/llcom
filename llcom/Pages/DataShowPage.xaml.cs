using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using llcom.ViewModels;

namespace llcom.Pages;

/// <summary>
/// DataShowPage.xaml 的交互逻辑（Step 8 MVVM 化）。
/// 显示逻辑全部移入 DataShowViewModel；本页只保留 UI 桥接
/// （流式文本框增量追加、滚动、分包/流式切换、保存日志对话框）。
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public partial class DataShowPage : Page
{
    private DataShowViewModel _vm;
    private bool loaded = false;

    public DataShowPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (loaded)
            return;
        loaded = true;

        //显示逻辑全部在 VM（订阅 Logger.DataShowTask 等）
        _vm = new DataShowViewModel(Tools.Global.setting);
        DataContext = _vm;

        //流式文本框桥接：增量追加保持性能，未锁定时滚动到底部
        _vm.SetTextBridge(
            t =>
            {
                MainTextBox.AppendText(t);
                if (!_vm.LockLog)
                    MainTextBox.ScrollToEnd();
            },
            () => MainTextBox.Clear()
        );

        //分包/流式显示切换
        _vm.PackModeChanged += pack =>
        {
            MainListScrollViewer.Visibility = pack ? Visibility.Visible : Visibility.Collapsed;
            MainTextBox.Visibility = pack ? Visibility.Collapsed : Visibility.Visible;
        };
        //分包模式新增条目后滚动到底部
        _vm.ScrollRequested += () =>
        {
            if (!_vm.LockLog)
                MainListScrollViewer.ScrollToEnd();
        };

        //初始显示模式（timeout≥0 分包，否则流式）
        var needPack = Tools.Global.setting.timeout >= 0;
        MainListScrollViewer.Visibility = needPack ? Visibility.Visible : Visibility.Collapsed;
        MainTextBox.Visibility = needPack ? Visibility.Collapsed : Visibility.Visible;
    }

    private void LockLogButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.LockLog = !_vm.LockLog;
    }

    private void SaveLogButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Log files(*.log)|*.log";
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            _vm.SaveLog(saveFileDialog.FileName);
        }
    }
}
