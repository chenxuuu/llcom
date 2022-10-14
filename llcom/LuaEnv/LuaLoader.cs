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
            //输入框
            lua.DoString("apiInputBox = CS.llcom.LuaEnv.LuaApis.InputBox");
            //加点
            lua.DoString("apiAddPoint = CS.llcom.LuaEnv.LuaApis.AddPoint");

            if (t != "send")
            {
                //发送串口数据
                lua.DoString("apiSendUartData = CS.llcom.LuaEnv.LuaApis.SendUartData");
                //定时器
                lua.DoString("apiStartTimer = CS.llcom.LuaEnv.LuaRunEnv.StartTimer");
                lua.DoString("apiStopTimer = CS.llcom.LuaEnv.LuaRunEnv.StopTimer");
            }

            //加上需要require的路径
            lua.DoString(@"
local rootPath = '"+ LuaApis.Utf8ToAsciiHex(LuaApis.GetPath()) + @"'
rootPath = rootPath:gsub('[%s%p]', ''):upper()
rootPath = rootPath:gsub('%x%x', function(c)
                                    return string.char(tonumber(c, 16))
                                end)
package.path = package.path..
';'..rootPath..'core_script/?.lua'..
';'..rootPath..'?.lua'..
';'..rootPath..'user_script_run/requires/?.lua'
package.cpath = package.cpath..
';'..rootPath..'core_script/?.lua'..
';'..rootPath..'?.lua'..
';'..rootPath..'user_script_run/requires/?.lua'
");

            //运行初始化文件
            lua.DoString("require 'core_script.head'");
        }


        private static XLua.LuaEnv luaRunner = null;
        /// <summary>
        /// 运行lua文件并获取结果
        /// </summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static byte[] Run(string file, ArrayList args = null, string path = "user_script_send_convert/")
        {
            //文件不存在
            if (!File.Exists(Tools.Global.ProfilePath + path + file))
                return new byte[] { };

            if (luaRunner == null)
            {
                luaRunner = new XLua.LuaEnv();
                luaRunner.Global.SetInPath("runType", "send");//一次性处理标志
                Initial(luaRunner, "send");
            }
            lock (luaRunner)
            {
                luaRunner.Global.SetInPath("file", path + file);

                try
                {
                    if (args != null)
                        for (int i = 0; i < args.Count; i += 2)
                        {
                            luaRunner.Global.SetInPath((string)args[i], args[i + 1]);
                            System.Diagnostics.Debug.WriteLine($"{(string)args[i]},{args[i + 1]}");
                        }
                    var r = luaRunner.DoString("return require('core_script.once')()")[0].ToString();
                    return Tools.Global.Hex2Byte(r);
                }
                catch (Exception e)
                {
                    luaRunner.Dispose();
                    luaRunner = null;
                    throw new Exception(e.ToString());
                }
            }
        }
    }
}
