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
        public static void Initial(NLua.Lua lua)
        {
            //utf8转gbk编码的hex值
            lua.RegisterFunction("apiUtf8ToHex", null, typeof(LuaApis).GetMethod("Utf8ToAsciiHex")); 
            //获取软件目录路径
            lua.RegisterFunction("apiGetPath", null, typeof(LuaApis).GetMethod("GetPath"));
            //发送串口数据
            if((lua["runType"] as string) != "send")
            {
                lua.RegisterFunction("apiSendUartData", null, typeof(LuaApis).GetMethod("SendUartData"));
                lua.RegisterFunction("apiStartTimer", null, typeof(LuaRunEnv).GetMethod("StartTimer")); 
                lua.RegisterFunction("apiStopTimer", null, typeof(LuaRunEnv).GetMethod("StopTimer"));
            }
            //输出日志
            lua.RegisterFunction("apiPrintLog", null, typeof(LuaApis).GetMethod("PrintLog"));

            //运行初始化文件
            lua.DoFile("core_script/head.lua");
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
            if (!File.Exists(file))
                return "";

            using (var lua = new NLua.Lua())
            {
                try
                {
                    lua.State.Encoding = Encoding.UTF8;
                    lua.LoadCLRPackage();
                    lua["runType"] = "send";//一次性处理标志
                    Initial(lua);
                    if (args != null)
                        for (int i = 0; i < args.Count; i += 2)
                        {
                            lua[(string)args[i]] = args[i + 1];
                        }
                    return (string)lua.DoFile(file)[0];
                }
                catch (Exception e)
                {
                    throw new Exception(e.ToString());
                }
            }
        }
    }
}
