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
        private static Dictionary<int,ArrayList> pool = new Dictionary<int, ArrayList>();//回调池子
        private static List<ArrayList> toRun = new List<ArrayList>();//待运行的池子
        public static bool isRunning = false;
        public static bool canRun = false;

        /// <summary>
        /// 刚启动的时候运行的
        /// </summary>
        public static void init()
        {
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
        }

        private static void addTigger(int id, string type = "timer", string data = "")
        {
            toRun.Add(new ArrayList { id, type, data });
        }


        /// <summary>
        /// 实时跑一段lua代码
        /// </summary>
        /// <param name="l"></param>
        public static void RunCommand(string l)
        {
            addTigger(-1, "cmd", l);
        }


        /// <summary>
        /// 收到串口消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            addTigger(-1,"uartRev", Tools.Global.Byte2Hex(sender as byte[]));
        }

        private static void runTigger()
        {
            try
            {
                while (true)
                {
                    Task.Delay(1).Wait();
                    if (tokenSource.IsCancellationRequested)
                        return;
                    if (toRun.Count > 0)
                    {
                        lua["sys.inputData"] = toRun[0][2];
                        lua.DoString($"tiggerCB({toRun[0][0]},{toRun[0][1]},sys.inputData)");
                        //lua.GetFunction("tiggerCB").Call(toRun[0][0], toRun[0][1], toRun[0][2]);
                        toRun.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                StopLua(ex.ToString());
            }
        }

        /// <summary>
        /// 新建定时器
        /// </summary>
        /// <param name="id">编号</param>
        /// <param name="time">时间(ms)</param>
        public static int StartTimer(int id,int time)
        {
            CancellationTokenSource timerToken = new CancellationTokenSource();
            pool.Add(id, new ArrayList { "timer","",timerToken });
            Task.Run(() => 
            {
                Task.Delay(time).Wait();
                if (timerToken == null || timerToken.IsCancellationRequested)
                    return;
                addTigger(id);
                pool.Remove(id);
            }, timerToken.Token);
            return 1;
        }

        /// <summary>
        /// 停止定时器
        /// </summary>
        /// <param name="id">编号</param>
        public static void StopTimer(int id)
        {
            if(pool[id] != null)
            {
                Task.Run(() =>
                {
                    ((CancellationTokenSource)pool[id][2]).Cancel();
                    pool.Remove(id);
                });
            }
        }

        /// <summary>
        /// 停止运行lua
        /// </summary>
        public static void StopLua(string ex)
        {
            LuaRunError(null, EventArgs.Empty);
            if (ex != "")
                LuaApis.PrintLog("lua代码报错了：\r\n" + ex);
            else
                LuaApis.PrintLog("lua代码已停止");
            tokenSource.Cancel();
            foreach(var i in pool)
            {
                if((string)i.Value[0] == "timer")
                {
                    ((CancellationTokenSource)i.Value[2]).Cancel();
                }
            }
            Task.Run(() =>
            {
                Task.Delay(100).Wait();
                pool.Clear();
            });
            if(lua.State != null)
            {
                lua["runMaxSeconds"] = 0;
            }
            lua.Dispose();
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
            
            //文件不存在
            if (!File.Exists(file))
                return;
            Task.Run(() =>
            {
                while(!canRun)
                    Task.Delay(100).Wait();
                try
                {
                    lua = new NLua.Lua();
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
                runTigger();
            }, tokenSource.Token);
        }
    }
}
