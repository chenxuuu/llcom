using System;
using System.Collections;
using System.Collections.Concurrent;
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
        private static XLua.LuaEnv lua = null;
        private static CancellationTokenSource tokenSource = null;
        private static ConcurrentDictionary<int, CancellationTokenSource> pool = 
            new ConcurrentDictionary<int, CancellationTokenSource>();//timer回调池子
        private static ConcurrentBag<LuaPool> toRun = new ConcurrentBag<LuaPool>();//待运行的池子

        public static bool isRunning = false;
        public static bool canRun = false;

        private static object tiggerLock = new object();//回调锁标志

        /// <summary>
        /// 刚启动的时候运行的
        /// </summary>
        public static void init()
        {
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
        }

        private static void addTigger(int id, string type = "timer", byte[] data = null)
        {
            if(isRunning)
            {
                toRun.Add(new LuaPool { id = id, type = type, data = data });
                runTigger();
            }
        }


        /// <summary>
        /// 实时跑一段lua代码
        /// </summary>
        /// <param name="l"></param>
        public static void RunCommand(string l)
        {
            addTigger(-1, "cmd", Encoding.UTF8.GetBytes(l));
        }


        /// <summary>
        /// 收到串口消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            addTigger(-1,"uartRev", sender as byte[]);
        }

        /// <summary>
        /// 收到通用通道消息
        /// </summary>
        public static void ChannelReceived(string channel, object data)
        {
            if (isRunning)
            {
                toRun.Add(new LuaPool { id = -1, type = channel, data = data });
                runTigger();
            }
        }

        private static void runTigger()
        {
            lock (tiggerLock)
            {
                try
                {
                    while (toRun.Count > 0)
                    {
                        if (tokenSource.IsCancellationRequested)
                            return;
                        while (toRun.Count > 0)
                        {
                            try
                            {
                                LuaPool temp;
                                toRun.TryTake(out temp);
                                lua.Global.Get<XLua.LuaFunction>("tiggerCB").Call(temp.id, temp.type, temp.data);
                            }
                            catch (Exception le)
                            {
                                LuaApis.PrintLog("回调报错：\r\n" + le.ToString());
                            }
                            if (tokenSource.IsCancellationRequested)
                                return;
                        }
                    }
                }
                catch (Exception ex)
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
        public static int StartTimer(int id,int time)
        {
            CancellationTokenSource timerToken = new CancellationTokenSource();
            if (pool.ContainsKey(id))
            {
                try
                {
                    CancellationTokenSource tc;
                    pool.TryRemove(id,out tc);
                    tc.Cancel();
                }
                catch { }
            }
            pool.TryAdd(id, timerToken);
            var timer = new System.Timers.Timer(time);
            timer.Elapsed += (sender, e) =>
            {
                if (timerToken == null || timerToken.IsCancellationRequested)
                    return;
                if (!isRunning)
                    return;
                pool.TryRemove(id, out _);
                addTigger(id);
                ((System.Timers.Timer)sender).Dispose();
            };
            timer.AutoReset = false;
            timer.Start();
            return 1;
        }

        /// <summary>
        /// 停止定时器
        /// </summary>
        /// <param name="id">编号</param>
        public static void StopTimer(int id)
        {
            if(pool.ContainsKey(id))
            {
                try
                {
                    CancellationTokenSource tc;
                    pool.TryRemove(id, out tc);
                    tc.Cancel();
                }
                catch { }
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
            foreach(var v in pool)
            {
                v.Value.Cancel();
            }
            isRunning = false;
            tokenSource.Cancel();
            pool.Clear();
            lua = null;
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
            if (!File.Exists(Tools.Global.ProfilePath + file))
                return;
            lua = new XLua.LuaEnv();
            Task.Run(() =>
            {
                while(!canRun)
                    Task.Delay(100).Wait();
                try
                {
                    lua.Global.SetInPath("runType", "script");//一次性处理标志
                    LuaLoader.Initial(lua);
                    lua.DoString($"require '{file.Replace("/",".").Substring(0,file.Length-4)}'");
                }
                catch (Exception ex)
                {
                    StopLua(ex.ToString());
                }
            }, tokenSource.Token);
        }
    }


    class LuaPool
    {
        public int id { get; set; }
        public string type { get; set; }
        public object data { get; set; }
    }
}
