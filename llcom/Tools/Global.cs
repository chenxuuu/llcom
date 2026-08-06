using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using llcom.Model;

namespace llcom.Tools
{
    /// <summary>
    /// 全局状态与外观层（组合根）。
    /// 经 Step 2 拆分后，本类只保留"全局可变状态 + 全局事件"，以及指向各服务的转发成员，
    /// 以保证所有旧调用点（Tools.Global.xxx）无需修改即可继续工作。
    /// 拆分后的职责归属：
    ///   - 路径/环境            → Services/AppPaths.cs
    ///   - 配置加载/初始化      → Services/ProfileInitializer.cs
    ///   - 编码/Hex 转换        → Services/EncodingHelper.cs
    ///   - 语言切换             → Services/LocalizationService.cs
    ///   - 在线脚本             → Services/OnlineScriptService.cs
    ///   - 文件工具             → Services/FileUtils.cs
    /// </summary>
    class Global
    {
        public static event EventHandler ProgramClosedEvent;
        //api接口文档网址
        public static string apiDocumentUrl = "https://github.com/chenxuuu/llcom/blob/master/LuaApi.md";
        //主窗口是否被关闭？
        private static bool _isMainWindowsClosed = false;
        public static bool isMainWindowsClosed
        {
            get
            {
                return _isMainWindowsClosed;
            }
            set
            {
                _isMainWindowsClosed = value;
                if (value)
                {
                    setting?.Flush();//防抖队列中的修改强制落盘
                    uart.WaitUartReceive.Set();
                    Logger.CloseUartLog();
                    Logger.CloseLuaLog();
                    if (File.Exists(ProfilePath + "lock"))
                        File.Delete(ProfilePath + "lock");
                    ProgramClosedEvent?.Invoke(null, EventArgs.Empty);
                }
            }
        }
        //给全局使用的设置参数项
        public static Model.Settings setting;
        public static Services.ISerialPortService uart = new Services.SerialPortService();

        /// <summary>
        /// 是否上报bug？低版本.net框架的上报行为将被限制
        /// </summary>
        public static bool ReportBug { get; set; } = true;

        /// <summary>
        /// 是否有新版本？
        /// </summary>
        public static bool HasNewVersion { get; set; } = false;

        /// <summary>
        /// 用户当前选择的接收转换脚本（发送时恢复用；逐条指定接收脚本时不覆盖此值）
        /// </summary>
        public static string recvScriptBackup = "";

        /// <summary>
        /// 更换软件标题栏文字
        /// </summary>
        public static event EventHandler<string> ChangeTitleEvent;
        public static void ChangeTitle(string s) => ChangeTitleEvent?.Invoke(null, s);

        /// <summary>
        /// 刷新lua脚本列表
        /// </summary>
        public static event EventHandler RefreshLuaScriptListEvent;
        public static void RefreshLuaScriptList() => RefreshLuaScriptListEvent?.Invoke(null, null);

        // ========== 以下为指向各服务的转发成员（保持旧调用点不变） ==========

        //软件文件名
        public static string FileName => AppPaths.FileName;
        /// <summary>
        /// 软件根目录（末尾带\）
        /// </summary>
        public static string AppPath => AppPaths.AppPath;
        //配置文件路径（普通exe时，会被替换为AppPath）
        public static string ProfilePath
        {
            get => AppPaths.ProfilePath;
            set => AppPaths.ProfilePath = value;
        }

        /// <summary>
        /// 获取实际的ProfilePath路径（目前没啥用了）
        /// </summary>
        /// <returns></returns>
        public static string GetTrueProfilePath() => AppPaths.GetTrueProfilePath();

        /// <summary>
        /// 是否为应用商店版本？
        /// </summary>
        /// <returns></returns>
        public static bool IsMSIX() => AppPaths.IsMSIX();

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadSetting() => ProfileInitializer.LoadSetting();

        /// <summary>
        /// 软件打开后，所有东西的初始化流程
        /// </summary>
        public static void Initial() => ProfileInitializer.Initial();

        public static Encoding GetEncoding() => EncodingHelper.GetEncoding();

        /// <summary>
        /// 字符串转hex值
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="space">间隔符号</param>
        /// <returns>结果</returns>
        public static string String2Hex(string str, string space) => EncodingHelper.String2Hex(str, space);

        /// <summary>
        /// hex值转字符串
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static string Hex2String(string mHex) => EncodingHelper.Hex2String(mHex);

        /// <summary>
        /// byte转string
        /// </summary>
        /// <param name="vBytes"></param>
        /// <returns></returns>
        public static string Byte2String(byte[] vBytes, int len = -1) => EncodingHelper.Byte2String(vBytes, len);

        /// <summary>
        /// byte转string（可读）
        /// </summary>
        /// <param name="vBytes"></param>
        /// <returns></returns>
        public static string Byte2Readable(byte[] vBytes, int len = -1) => EncodingHelper.Byte2Readable(vBytes, len);

        /// <summary>
        /// hex转byte
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static byte[] Hex2Byte(string mHex) => EncodingHelper.Hex2Byte(mHex);

        /// <summary>
        /// byte转hex
        /// </summary>
        /// <param name="d"></param>
        /// <param name="s"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string Byte2Hex(byte[] d, string s = "", int len = -1) => EncodingHelper.Byte2Hex(d, s, len);

        /// <summary>
        /// 导入SSCOM配置文件数据
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Model.ToSendData> ImportFromSSCOM(string path) => FileUtils.ImportFromSSCOM(path);

        /// <summary>
        /// 读取软件资源文件内容
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>内容字节数组</returns>
        public static byte[] GetAssetsFileContent(string path) => FileUtils.GetAssetsFileContent(path);

        /// <summary>
        /// 取出文件
        /// </summary>
        /// <param name="insidePath">软件内部的路径</param>
        /// <param name="outPath">需要释放到的路径</param>
        /// <param name="d">是否覆盖</param>
        public static void CreateFile(string insidePath, string outPath, bool d = true) => FileUtils.CreateFile(insidePath, outPath, d);

        /// <summary>
        /// 更换语言文件
        /// </summary>
        /// <param name="languagefileName"></param>
        public static void LoadLanguageFile(string languagefileName) => LocalizationService.LoadLanguageFile(languagefileName);

        /// <summary>
        /// 获取在线脚本
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static List<OnlineScript> GetOnlineScripts(Action<int, int> callback = null) => OnlineScriptService.GetOnlineScripts(callback);
    }
}
