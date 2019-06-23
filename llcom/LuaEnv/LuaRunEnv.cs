using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace llcom.LuaEnv
{
    class LuaRunEnv
    {
        public static event EventHandler LuaRunError;//报错的回调
        private static NLua.Lua lua = null;
        private static CancellationTokenSource tokenSource = null;
        private static ArrayList pool = new ArrayList();//定时器池子
        private static bool timerRunning = false;
        public static bool isRunning = false;
        public static bool canRun = false;

        /// <summary>
        /// 刚启动的时候运行的
        /// </summary>
        public static void init()
        {
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
        }


        /// <summary>
        /// 收到串口消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                runTigger(-1,"uartRev",sender as string);
            }, tokenSource.Token);
        }

        private static void runTigger(int id,string type="timer",string data="")
        {
            if (timerRunning)
            {
                pool.Add(id);
                pool.Add(type);
                pool.Add(data);
                return;
            }
            else
            {
                pool.Add(id);
                pool.Add(type);
                pool.Add(data);
                try
                {
                    while(pool.Count > 0)
                    {
                        lua.GetFunction("tiggerCB").Call(pool[0],pool[1],pool[2]);
                        pool.RemoveAt(0);
                        pool.RemoveAt(0);
                        pool.RemoveAt(0);
                    }
                }
                catch(Exception ex)
                {
                    StopLua(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 新建定时器
        /// </summary>
        /// <param name="id">编号</param>
        /// <param name="time">时间(ms)</param>
        public static void StartTimer(int id,int time)
        {
            Task.Run(() => 
            {
                Task.Delay(time).Wait();
                runTigger(id);
            }, tokenSource.Token);
        }

        /// <summary>
        /// 停止运行lua
        /// </summary>
        public static void StopLua(string ex)
        {
            if(ex != "")
                LuaApis.PrintLog("lua代码报错了：\r\n" + ex);
            else
                LuaApis.PrintLog("lua代码已停止");
            tokenSource.Cancel();
            pool.Clear();
            if(lua.State != null)
            {
                lua["runMaxSeconds"] = 0;
            }
            lua.Dispose();
            LuaRunError(null, EventArgs.Empty);
            isRunning = false;
        }

        /// <summary>
        /// 新建一个新的lua虚拟机
        /// </summary>
        public static void New(string file)
        {
            canRun = false;
            isRunning = true;
            if (tokenSource != null)
                tokenSource.Dispose();
            tokenSource = new CancellationTokenSource();//task取消指示
            lua = new NLua.Lua();
            //文件不存在
            if (!File.Exists(file))
                return;
            Task.Run(() =>
            {
                while(!canRun)
                    Task.Delay(100).Wait();
                try
                {
                    lua.State.Encoding = Encoding.UTF8;
                    lua.LoadCLRPackage();
                    lua["runType"] = "script";//一次性处理标志
                    LuaLoader.Initial(lua);
                    lua.DoFile(file);
                }
                catch (Exception ex)
                {
                    StopLua(ex.ToString());
                }
            }, tokenSource.Token);
        }
    }
}
