using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace llcom.ViewModels
{
    /// <summary>
    /// Lua 脚本编辑器 ViewModel（Step 7）。
    /// 通用组件：主窗口的 user_script_run 编辑器、设置窗口的 send/recv_convert 编辑器共用，
    /// 消除原 MainWindow/SettingWindow 中的重复实现。
    /// 职责：文件列表加载、打开/保存/新建、自动保存、外部修改检测、脚本重载回调。
    /// 运行/日志等主窗口特有逻辑保留在 code-behind。
    /// </summary>
    public partial class LuaEditorViewModel : ObservableObject
    {
        private readonly string _folderName;            //脚本目录（相对 ProfilePath），如 "user_script_run/"
        private readonly Func<string> _getCurrentScript;//当前选中脚本名（对应 setting 属性）
        private readonly Action<string> _setCurrentScript;//设置当前选中脚本名
        private readonly Action _onReload;              //脚本重载回调（如 LuaLoader.ClearRun）
        private readonly Action<string> _onLoaded;      //加载完成回调（如设置 recvScriptBackup）
        //编辑器文本访问桥（AvalonEdit Text 不可绑定，由 View 通过 SetTextBridge 接入；
        //接入前使用内部缓冲，避免 VM 持有 UI 控件引用）
        private Func<string> _getText;
        private Action<string> _setText;
        private string _fallbackText = "";

        /// <summary>脚本文件列表（不带 .lua 后缀）</summary>
        public ObservableCollection<string> Files { get; } = new ObservableCollection<string>();
        /// <summary>文件列表选中项（绑定 ComboBox）</summary>
        [ObservableProperty] private string _selectedFile = "";

        //加载锁 + 文件时间戳（自动保存 / 外部修改检测依据）
        private bool _loading;
        private string _lastFile = "";
        private DateTime _lastFileTime;
        private DateTime _lastChangeTime;

        public LuaEditorViewModel(string folderName,
            Func<string> getCurrent, Action<string> setCurrent,
            Action onReload = null, Action<string> onLoaded = null)
        {
            _folderName = folderName;
            _getCurrentScript = getCurrent;
            _setCurrentScript = setCurrent;
            _getText = () => _fallbackText;
            _setText = t => _fallbackText = t;
            _onReload = onReload ?? (() => { });
            _onLoaded = onLoaded ?? (_ => { });
            RefreshList();
            LoadFile(getCurrent());
        }

        /// <summary>
        /// 由 View 接入编辑器文本读写（AvalonEdit TextEditor.Text）。
        /// 接入时会把已加载的缓冲文本同步到控件。
        /// </summary>
        public void SetTextBridge(Func<string> getText, Action<string> setText)
        {
            _getText = getText;
            _setText = setText;
            setText(_fallbackText);
        }

        /// <summary>脚本完整路径</summary>
        private string FullPath(string fileName) => Tools.Global.ProfilePath + _folderName + fileName + ".lua";

        /// <summary>
        /// 刷新文件列表（脚本目录下的 .lua 文件）
        /// </summary>
        public void RefreshList()
        {
            var dir = new DirectoryInfo(Tools.Global.ProfilePath + _folderName);
            _loading = true;
            Files.Clear();
            foreach (var f in dir.GetFileSystemInfos())
            {
                if (f is FileInfo file && file.Name.ToLower().EndsWith(".lua"))
                    Files.Add(file.Name.Substring(0, file.Name.Length - 4));
            }
            _loading = false;
            _lastFile = _getCurrentScript();
            SelectedFile = _lastFile;
        }

        /// <summary>
        /// 加载脚本文件；不存在时回退到 "default"（若 default 也不存在则创建）
        /// </summary>
        public void LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;
            if (!File.Exists(FullPath(fileName)))
            {
                _setCurrentScript("default");
                if (!File.Exists(FullPath("default")))
                    File.Create(FullPath("default")).Close();
            }
            else
            {
                _setCurrentScript(fileName);
            }

            try
            {
                _setText(File.ReadAllText(FullPath(_getCurrentScript())));
            }
            catch
            {
                Tools.MessageBox.Show("File load failed.\r\n" +
                    "Do not open this file in other application!");
                return;
            }

            //记录最后时间；修改时间使用文件时间
            _lastFileTime = File.GetLastWriteTime(FullPath(_getCurrentScript()));
            _lastChangeTime = _lastFileTime;

            RefreshList();
            _onReload();//脚本已变更，通知重载（发送/接收转换脚本缓存）
            _onLoaded(_getCurrentScript());
        }

        /// <summary>
        /// 保存脚本文件（仅在编辑器有改动时写盘，避免无谓 IO）
        /// </summary>
        public void SaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;
            try
            {
                if (_lastChangeTime > _lastFileTime)
                {
                    File.WriteAllText(FullPath(fileName), _getText());
                    _lastFileTime = File.GetLastWriteTime(FullPath(fileName));
                }
            }
            catch { }
        }

        /// <summary>编辑器文本变化（标记已修改，供自动保存判断）</summary>
        public void OnTextChanged() => _lastChangeTime = DateTime.Now;

        /// <summary>失焦/窗口切换/关闭时的自动保存</summary>
        public void OnAutoSave() => SaveFile(_lastFile);

        /// <summary>窗口激活时检测外部修改（其他编辑器改过则重新加载）</summary>
        public void CheckExternalChange()
        {
            if (string.IsNullOrEmpty(_lastFile))
                return;
            var fileTime = File.GetLastWriteTime(FullPath(_lastFile));
            if (fileTime > _lastFileTime)
                LoadFile(_lastFile);
        }

        /// <summary>
        /// 新建脚本（重名/非法名提示）
        /// </summary>
        public void CreateNew(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Tools.MessageBox.Show((Application.Current?.TryFindResource("LuaNoName") as string) ?? "?!");
                return;
            }
            if (File.Exists(FullPath(fileName)))
            {
                Tools.MessageBox.Show((Application.Current?.TryFindResource("LuaExist") as string) ?? "?!");
                return;
            }
            try
            {
                File.Create(FullPath(fileName)).Close();
                LoadFile(fileName);
            }
            catch
            {
                Tools.MessageBox.Show((Application.Current?.TryFindResource("LuaCreateFail") as string) ?? "?!");
            }
        }

        /// <summary>
        /// 文件列表选中变化：保存上一个文件，加载新文件
        /// </summary>
        partial void OnSelectedFileChanged(string value)
        {
            if (value == null || _loading)
                return;
            if (_lastFile != "")
                SaveFile(_lastFile);
            LoadFile(value);
        }
    }
}
