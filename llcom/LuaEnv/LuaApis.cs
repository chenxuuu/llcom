using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.LuaEnv
{
    class LuaApis
    {
        public static event EventHandler PrintLuaLog;
        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="log">日志内容</param>
        public static void PrintLog(string log)
        {
            Tools.Logger.AddLuaLog(log);
            PrintLuaLog(DateTime.Now.ToString("[HH:mm:ss:ffff]")+log, EventArgs.Empty);
        }

        /// <summary>
        /// utf8编码改为gbk的hex编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Utf8ToAsciiHex(string input)
        {
            return BitConverter.ToString(Encoding.GetEncoding("GB2312").GetBytes(input)).Replace("-","");
        }

        /// <summary>
        /// 获取程序运行目录
        /// </summary>
        /// <returns>主程序运行目录（win10商店时返回appdata路径）</returns>
        public static string GetPath()
        {
            return Tools.Global.ProfilePath;
        }

        /// <summary>
        /// 发送串口数据
        /// </summary>
        /// <returns>是否成功</returns>
        public static bool SendUartData(string data)
        {
            if (Tools.Global.uart.IsOpen())
            {
                try
                {
                    Tools.Global.uart.SendData(Tools.Global.Hex2Byte(data));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取快捷发送区数据
        /// </summary>
        /// <param name="id">快捷发送数据的编号</param>
        /// <returns>内容，如果不存在则为空字符串</returns>
        public static string QuickSendList(int id)
        {
            if (Tools.Global.setting.quickSend.Count < id || id <= 0)
                return "";
            else if (Tools.Global.setting.quickSend[id - 1].hex)
                return "H" + Tools.Global.setting.quickSend[id - 1].text;
            else
                return "S" + Tools.Global.setting.quickSend[id - 1].text;
        }

        /// <summary>
        /// 输入框
        /// </summary>
        /// <returns></returns>
        public static bool InputBox(string prompt, out string returnValue, string defaultInput = "", string title = null)
        {
            Tuple<bool, string> ret = App.Current.Dispatcher.Invoke(() => {
                return Tools.InputDialog.OpenDialog(prompt, defaultInput, title);
            });
            returnValue = ret.Item2;
            return ret.Item1;
        }


        public static event EventHandler<Model.LinePlotPoint> LinePlotAdd;
        /// <summary>
        /// 画点
        /// </summary>
        /// <param name="n">值</param>
        /// <param name="l">哪根线</param>
        public static void AddPoint(double n, int l)
        {
            LinePlotAdd?.Invoke(null, new Model.LinePlotPoint { N = n, Line = l });
        }
    }
}
