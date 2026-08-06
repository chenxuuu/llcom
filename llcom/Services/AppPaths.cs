using System;
using System.Diagnostics;
using System.IO;

namespace llcom.Tools
{
    /// <summary>
    /// 应用路径与环境信息服务。
    /// 负责软件文件名、运行目录、配置目录（ProfilePath）、商店版（MSIX）判断。
    /// 从原 Tools/Global.cs 拆出（Step 2），Global 外观层转发同名成员，调用点无感知。
    /// </summary>
    internal static class AppPaths
    {
        //软件文件名
        private static string _fileName = "";
        public static string FileName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_fileName))
                {
                    using (var processModule = Process.GetCurrentProcess().MainModule)
                    {
                        _fileName = System.IO.Path.GetFileName(processModule?.FileName);
                    }
                }
                return _fileName;
            }
        }

        //软件根目录
        private static string _appPath = null;
        /// <summary>
        /// 软件根目录（末尾带\）
        /// </summary>
        public static string AppPath
        {
            get
            {
                if (_appPath == null)
                {
                    using (var processModule = Process.GetCurrentProcess().MainModule)
                    {
                        _appPath = System.IO.Path.GetDirectoryName(processModule?.FileName);
                    }
                    if (!_appPath.EndsWith("\\"))
                        _appPath = _appPath + "\\";
                }
                return _appPath;
            }
        }

        //配置文件路径（普通exe时，会被替换为AppPath）
        public static string ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\llcom\";

        /// <summary>
        /// 获取实际的ProfilePath路径（目前没啥用了）
        /// </summary>
        /// <returns></returns>
        public static string GetTrueProfilePath()
        {
            return ProfilePath;
        }

        /// <summary>
        /// 是否为应用商店版本？
        /// </summary>
        /// <returns></returns>
        public static bool IsMSIX()
        {
            return AppPath.ToUpper().Contains(@"\PROGRAM FILES\WINDOWSAPPS\");
        }
    }
}
