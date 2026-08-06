using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using llcom.Model;

namespace llcom.ViewModels
{
    /// <summary>
    /// 快捷发送区页面条目（菜单项）：页名 + 页索引。
    /// </summary>
    public class QuickSendPage
    {
        public string Name { get; set; }
        public int Index { get; }
        public QuickSendPage(int index, string name) { Index = index; Name = name; }
        public override string ToString() => Name;
    }

    /// <summary>
    /// 快捷发送区 ViewModel（Step 6）。
    /// 职责：10 页列表数据管理（加载/保存/增删/排序/清空/切页/改名/JSON与SSCOM导入导出）。
    /// 逻辑从 MainWindow code-behind 迁移，行为保持一致。
    /// 注意：单条目的接收脚本/参数弹窗（依赖 Popup 控件）与条目发送按钮保留在 code-behind。
    /// </summary>
    public partial class QuickSendViewModel : ObservableObject
    {
        private readonly Model.Settings _setting;

        /// <summary>当前页条目列表（绑定 ListBox）</summary>
        public ObservableCollection<ToSendData> Items { get; } = new ObservableCollection<ToSendData>();
        /// <summary>10 页菜单列表（绑定页面切换菜单）</summary>
        public ObservableCollection<QuickSendPage> Pages { get; } = new ObservableCollection<QuickSendPage>();

        [ObservableProperty] private string _currentPageName = "";
        [ObservableProperty] private int _currentPageIndex;

        //加载/保存过程中的锁（等价原 canSaveSendList，防止加载期间误保存）
        private bool _canSave = true;

        public IRelayCommand AddCommand { get; }
        public IRelayCommand RemoveLastCommand { get; }
        public IRelayCommand RemoveAllCommand { get; }
        public IRelayCommand ImportCommand { get; }
        public IRelayCommand ExportCommand { get; }
        public IRelayCommand ImportSSCOMCommand { get; }
        public IRelayCommand SwitchPageCommand { get; }
        public IRelayCommand RenamePageCommand { get; }

        internal QuickSendViewModel(Model.Settings setting)
        {
            _setting = setting;

            AddCommand = new RelayCommand(Add);
            RemoveLastCommand = new RelayCommand(RemoveLast);
            RemoveAllCommand = new RelayCommand(RemoveAll);
            ImportCommand = new RelayCommand(Import);
            ExportCommand = new RelayCommand(Export);
            ImportSSCOMCommand = new RelayCommand(ImportSSCOM);
            SwitchPageCommand = new RelayCommand<int>(SwitchPage);
            RenamePageCommand = new RelayCommand(RenamePage);

            //条目属性变化（text/hex/commit 等）时自动保存
            ToSendData.DataChanged += (_, _) => Save();
            //页面名称变化（quickListName0-9）时刷新菜单
            setting.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null && e.PropertyName.StartsWith("quickListName"))
                    RefreshPages();
            };

            RefreshPages();
            //加载当前选中页
            if (setting.quickSendSelect == -1)
                setting.quickSendSelect = 0;
            CurrentPageIndex = setting.quickSendSelect;
            LoadCurrentPage();
        }

        /// <summary>
        /// 取本地化资源字符串（等价 Window.TryFindResource）
        /// </summary>
        private static string Localize(string key) => Application.Current?.TryFindResource(key) as string ?? "?!";

        /// <summary>
        /// 刷新页面菜单（10 页名称）
        /// </summary>
        private void RefreshPages()
        {
            Pages.Clear();
            for (int i = 0; i < 10; i++)
                Pages.Add(new QuickSendPage(i, _setting.GetQuickListNameByIndex(i)));
        }

        /// <summary>
        /// 加载当前选中页的数据（首次/切页时调用）
        /// </summary>
        public void LoadCurrentPage()
        {
            Items.Clear();
            if (_setting.quickSend.Count == 0)
            {
                _setting.quickSend = new System.Collections.Generic.List<ToSendData>
                {
                    new ToSendData{id = 1,text="example string",commit="右击更改此处文字",hex=false},
                    new ToSendData{id = 2,text="lua可通过接口获取此处数据",hex=false},
                    new ToSendData{id = 3,text="aa 01 02 0d 0a",commit="Hex数据也能发",hex=true},
                    new ToSendData{id = 4,text="此处数据会被lua处理",hex=false},
                    new ToSendData{id = 5,text="右击序号可以更改这一行的位置",hex=false},
                    new ToSendData{id = 6,text="",hex=false},
                };
            }
            foreach (var i in _setting.quickSend)
            {
                if (i.commit == null)
                    i.commit = Localize("QuickSendButton");
                Items.Add(i);
            }
            CheckIds();
            CurrentPageName = _setting.GetQuickListNameNow();
        }

        /// <summary>
        /// 保存当前页数据到设置（并持久化）
        /// </summary>
        public void Save()
        {
            if (!_canSave)
                return;
            CheckIds();
            var newList = new System.Collections.Generic.List<ToSendData>();
            foreach (var i in Items)
                newList.Add(i);
            _setting.quickSend = newList;
        }

        /// <summary>
        /// 检查并更正当前页条目序号（id 连续 1..N）
        /// </summary>
        public void CheckIds()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].id != i + 1)
                {
                    var item = Items[i];
                    Items.RemoveAt(i);//元素删掉重新加进去
                    item.id = i + 1;
                    Items.Insert(i, item);
                }
            }
        }

        /// <summary>
        /// 切换到指定页
        /// </summary>
        private void SwitchPage(int index)
        {
            if (index < 0 || index >= 10)
                return;
            _canSave = false;
            Items.Clear();
            _setting.quickSendSelect = index;
            LoadCurrentPage();
            CurrentPageIndex = index;
            _canSave = true;
        }

        /// <summary>
        /// 添加新条目
        /// </summary>
        private void Add()
        {
            Items.Add(new ToSendData { id = Items.Count + 1, text = "", hex = false, commit = Localize("QuickSendButton") });
            Save();
        }

        /// <summary>
        /// 删除最后一条
        /// </summary>
        private void RemoveLast()
        {
            if (Items.Count > 0)
                Items.RemoveAt(Items.Count - 1);
            Save();
        }

        /// <summary>
        /// 一键清空当前页（需输入 YES 确认）
        /// </summary>
        private void RemoveAll()
        {
            var (r, s) = Tools.InputDialog.OpenDialog(
                Localize("DeleteConfirmationMsg"), "", Localize("DeleteConfirmation"));
            if (r && s == "YES")
            {
                Items.Clear();
                Save();
            }
        }

        /// <summary>
        /// 右键当前页名称改名
        /// </summary>
        private void RenamePage()
        {
            var ret = Tools.InputDialog.OpenDialog(
                "↓↓↓↓↓↓", _setting.GetQuickListNameNow(), Localize("QuickSendListNameChangeTip"));
            if (!ret.Item1)
                return;
            _setting.SetQuickListNameNow(ret.Item2);
            CurrentPageName = ret.Item2;
            RefreshPages();
        }

        /// <summary>
        /// 导入 JSON 快捷发送数据（追加到当前页）
        /// </summary>
        private void Import()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = Localize("QuickSendLLCOMFile");
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            System.Collections.Generic.List<ToSendData> data;
            try
            {
                data = JsonConvert.DeserializeObject<System.Collections.Generic.List<ToSendData>>(
                    File.ReadAllText(dialog.FileName));
            }
            catch (Exception err)
            {
                Tools.MessageBox.Show(err.Message);
                return;
            }
            _canSave = false;
            foreach (var d in data)
                Items.Add(d);
            _canSave = true;
            Save();
        }

        /// <summary>
        /// 导出当前页为 JSON
        /// </summary>
        private void Export()
        {
            var dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.FileName = System.Text.RegularExpressions.Regex.Replace(CurrentPageName, "[<>/\\|:\"?*]", "-");
            dialog.Filter = Localize("QuickSendLLCOMFile");
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            try
            {
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(Items));
                Tools.MessageBox.Show(Localize("QuickSendSaveFileDone"));
            }
            catch (Exception err)
            {
                Tools.MessageBox.Show(err.Message);
            }
        }

        /// <summary>
        /// 从 SSCOM 配置文件导入（追加到当前页）
        /// </summary>
        private void ImportSSCOM()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = Localize("QuickSendSSCOMFile");
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            _canSave = false;
            foreach (var i in Tools.Global.ImportFromSSCOM(dialog.FileName))
            {
                Items.Add(new ToSendData
                {
                    id = Items.Count + 1,
                    text = i.text,
                    hex = i.hex,
                    commit = i.commit
                });
            }
            _canSave = true;
            Save();
        }
    }
}
