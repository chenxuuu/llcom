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
        public static void Initial(vJine.Lua.LuaContext lua, string t = "script")
        {
            //utf8转gbk编码的hex值
            lua.reg("apiUtf8ToHex", new Func<string,string>(LuaApis.Utf8ToAsciiHex));
            //获取软件目录路径
            lua.reg("apiGetPath", new Func<string>(LuaApis.GetPath));
            
            if(t != "send")
            {
                //发送串口数据
                lua.reg("apiSendUartData", new Func<string,bool>(LuaApis.SendUartData));
                //定时器
                lua.reg("apiStartTimer", new Func<int,int,int>(LuaRunEnv.StartTimer));
                lua.reg("apiStopTimer", new Action<int>(LuaRunEnv.StopTimer));
            }
            //输出日志
            lua.reg("apiPrintLog", new Action<string>(LuaApis.PrintLog));

            //运行初始化文件
            lua.load("core_script/head.lua");
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

            using (var lua = new vJine.Lua.LuaContext())
            {
                try
                {
                    lua.set("runType", "send");//一次性处理标志
                    lua.set("file", file);
                    Initial(lua, "send");
                    if (args != null)
                        for (int i = 0; i < args.Count; i += 2)
                        {
                            lua.set((string)args[i], args[i + 1].ToString());
                        }
                    return lua.load("core_script/once.lua")[0].ToString();
                }
                catch (Exception e)
                {
                    throw new Exception(e.ToString());
                }
            }
        }
    }
}
