using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.LuaEnv
{
    class LuaLoader
    {

        /// <summary>
        /// 初始化lua对象
        /// </summary>
        /// <param name="lua"></param>
        public static void Initial(XLua.LuaEnv lua, string t = "script")
        {
            //utf8转gbk编码的hex值
            lua.DoString("apiUtf8ToHex = CS.llcom.LuaEnv.LuaApis.Utf8ToAsciiHex");
            //获取软件目录路径
            lua.DoString("apiGetPath = CS.llcom.LuaEnv.LuaApis.GetPath");
            //输出日志
            lua.DoString("apiPrintLog = CS.llcom.LuaEnv.LuaApis.PrintLog");
            //获取快捷发送区数据
            lua.DoString("apiQuickSendList = CS.llcom.LuaEnv.LuaApis.QuickSendList");

            if (t != "send")
            {
                //发送串口数据
                lua.DoString("apiSendUartData = CS.llcom.LuaEnv.LuaApis.SendUartData");
                //定时器
                lua.DoString("apiStartTimer = CS.llcom.LuaEnv.LuaRunEnv.StartTimer");
                lua.DoString("apiStopTimer = CS.llcom.LuaEnv.LuaRunEnv.StopTimer");
            }

            //运行初始化文件
            lua.DoString("require 'core_script.head'");
        }

        /// <summary>
        /// 运行lua文件并获取结果
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Run(string file, ArrayList args = null)
        {
            //文件不存在
            if (!File.Exists("user_script_send_convert/" + file))
                return "";

            using (var lua = new XLua.LuaEnv())
            {
                try
                {
                    lua.Global.SetInPath("runType", "send");//一次性处理标志
                    lua.Global.SetInPath("file", file);
                    Initial(lua, "send");
                    if (args != null)
                        for (int i = 0; i < args.Count; i += 2)
                        {
                            lua.Global.SetInPath((string)args[i], args[i + 1].ToString());
                        }
                    return lua.DoString("return require'core_script.once'")[0].ToString();
                }
                catch (Exception e)
                {
                    throw new Exception(e.ToString());
                }
            }
        }
    }
}
