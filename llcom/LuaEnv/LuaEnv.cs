using llcom.LuaEnv;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace llcom.LuaEnv
{
    /// <summary>
    /// lua虚拟机对象
    /// 自带luat task框架接口
    /// </summary>
    public class LuaEnv : IDisposable
    {
        //lua虚拟机
        public XLua.LuaEnv lua;
        //报错的回调
        public event EventHandler<string> ErrorEvent;
        //打印日志的回调
        public event EventHandler<string> PrintEvent;
        //停止运行的回调
        public event EventHandler<bool> StopEvent;

        private bool stop = false;//是否停止运行
        private ConcurrentDictionary<int, CancellationTokenSource> timerPool =
                new ConcurrentDictionary<int, CancellationTokenSource>();//timer取消标志池子
        private static ConcurrentBag<LuaTaskData> toRun = new ConcurrentBag<LuaTaskData>();//待运行的池子
        private readonly object taskLock = new object();

        /// <summary>
        /// 发出报错信息
        /// </summary>
        /// <param name="msg">报错信息</param>
        private void error(string msg)
        {
            ErrorEvent?.Invoke(lua, msg);
        }

        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="msg">日志信息</param>
        public void print(string msg)
        {
            PrintEvent?.Invoke(lua, msg);
        }

        /// <summary>
        /// 添加一个回调触发
        /// </summary>
        /// <param name="type">触发类型</param>
        /// <param name="data">传递数据</param>
        public void addTigger(string type, object data) => addTask(-1, type, data);

        /// <summary>
        /// 添加一个
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        private void addTask(int id, string type, object data)
        {
            if (!stop)
            {
                toRun.Add(new LuaTaskData { id = id, type = type, data = data });
                runTask();
            }
        }

        /// <summary>
        /// 跑一遍任务池子里的任务
        /// </summary>
        private void runTask()
        {
            lock (taskLock)
                while (toRun.Count > 0)
                {
                    try
                    {
                        LuaTaskData task;
                        toRun.TryTake(out task);//取出来一个任务
                        var cb = lua.Global.Get<XLua.LuaTable>("sys").Get<XLua.LuaFunction>("tiggerCB");
                        cb.Call(task.id, task.type, task.data);//跑
                    }
                    catch (Exception e)
                    {
                        ErrorEvent?.Invoke(lua, e.Message);
                    }
                    if (stop)//任务停了
                        return;
                }
        }

        /// <summary>
        /// 新建定时器
        /// </summary>
        /// <param name="id">编号</param>
        /// <param name="time">时间(ms)</param>
        public int StartTimer(int id, int time)
        {
            CancellationTokenSource timerToken = new CancellationTokenSource();
            if (timerPool.ContainsKey(id))//如果已经有一个一样的定时器了？
                StopTimer(id);
            timerPool.TryAdd(id, timerToken);//加到池子里
            var timer = new System.Timers.Timer(time);
            timer.Elapsed += (sender, e) =>
            {
                if (timerToken == null || timerToken.IsCancellationRequested)
                    return;
                if (stop)
                    return;
                timerPool.TryRemove(id, out _);
                addTask(id, "timer", null);
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
        public void StopTimer(int id)
        {
            if (timerPool.ContainsKey(id))
            {
                try
                {
                    CancellationTokenSource tc;
                    timerPool.TryRemove(id, out tc);
                    tc.Cancel();
                }
                catch { }
            }
        }

        /// <summary>
        /// 跑代码
        /// </summary>
        /// <param name="s">代码</param>
        /// <returns>返回的结果</returns>
        public object[] DoString(string s)
        {
            try
            {
                lock (taskLock) return lua.DoString(s);
            }
            catch (Exception e)
            {
                ErrorEvent?.Invoke(lua, e.Message);
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 跑文件
        /// </summary>
        /// <param name="s">文件路径</param>
        /// <returns>返回的结果</returns>
        public object[] DoFile(string f)
        {
            try
            {
                var s = File.ReadAllBytes(f);
                lock (taskLock) return lua.DoString(s);
            }
            catch (Exception e)
            {
                ErrorEvent?.Invoke(lua, e.Message);
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 初始化，加载全部接口
        /// </summary>
        /// <param name="input">输入值，一般为匿名类</param>
        public LuaEnv(object input = null)
        {
            lua = new XLua.LuaEnv();
            if (input != null)
                lua.Global.SetInPath("lua", input);//传递输入值
            lock (taskLock) lua.DoString(sysCode);
            lua.Global.SetInPath("@this", this);//自己传给自己

            //加上需要require的路径
            lua.DoString(@"
local rootPath = '" + LuaApis.Utf8ToAsciiHex(LuaApis.GetPath()) + @"'
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
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            lock(taskLock)
            {
                stop = true;//停止待运行任务
                lua.Dispose();//释放lua虚拟机
                foreach (var v in timerPool)
                {
                    v.Value.Cancel();//取消所有timer
                }
                timerPool.Clear();//清空timer信息池子
                while (toRun.TryTake(out _)) ;//清空待运行池子
            }
            StopEvent?.Invoke(null, true);
        }


        private static string sysCode = @"
--加强随机数随机性
math.randomseed(tostring(os.time()):reverse():sub(1, 6))
--- 模块功能：Luat协程调度框架
-- @module sys
-- @author openLuat
-- @license MIT
-- @copyright openLuat
sys = {}
-- TaskID最大值
local TASK_TIMER_ID_MAX = 0x1FFFFFFF
-- msgId 最大值(请勿修改否则会发生msgId碰撞的危险)
local MSG_TIMER_ID_MAX = 0x7FFFFFFF
-- 任务定时器id
local taskTimerId = 0
-- 消息定时器id
local msgId = TASK_TIMER_ID_MAX
-- 定时器id表
local timerPool = {}
local taskTimerPool = {}
--消息定时器参数表
local para = {}
--定时器是否循环表
local loop = {}
--- Task任务延时函数，只能用于任务函数中
-- @number ms  整数，最大等待126322567毫秒
-- @return 定时结束返回nil,被其他线程唤起返回调用线程传入的参数
-- @usage sys.wait(30)
function sys.wait(ms)
    -- 参数检测，参数不能为负值
    assert(ms > 0, 'The wait time cannot be negative!')
    -- 选一个未使用的定时器ID给该任务线程
    while true do
        if taskTimerId >= TASK_TIMER_ID_MAX - 1 then
            taskTimerId = 0
        else
            taskTimerId = taskTimerId + 1
        end
        if taskTimerPool[taskTimerId] == nil then
            break
        end
    end
    local timerid = taskTimerId
    taskTimerPool[coroutine.running()] = timerid
    timerPool[timerid] = coroutine.running()
    -- 调用core的rtos定时器
    if 1 ~= _G['@this']:StartTimer(timerid, ms) then print('sys.StartTimer error') return end
    -- 挂起调用的任务线程
    local message = {coroutine.yield()}
    if #message ~= 0 then
        _G['@this']:StopTimer(timerid)
        taskTimerPool[coroutine.running()] = nil
        timerPool[timerid] = nil
        return table.unpack(message)
    end
end
--- Task任务的条件等待函数（包括事件消息和定时器消息等条件），只能用于任务函数中。
-- @param id 消息ID
-- @number ms 等待超时时间，单位ms，最大等待126322567毫秒
-- @return result 接收到消息返回true，超时返回false
-- @return data 接收到消息返回消息参数
-- @usage result, data = sys.waitUntil('SIM_IND', 120000)
function sys.waitUntil(id, ms)
    sys.subscribe(id, coroutine.running())
    local message = ms and {sys.wait(ms)} or {coroutine.yield()}
    sys.unsubscribe(id, coroutine.running())
    return message[1] ~= nil, table.unpack(message, 2, #message)
end
--- Task任务的条件等待函数扩展（包括事件消息和定时器消息等条件），只能用于任务函数中。
-- @param id 消息ID
-- @number ms 等待超时时间，单位ms，最大等待126322567毫秒
-- @return message 接收到消息返回message，超时返回false
-- @return data 接收到消息返回消息参数
-- @usage result, data = sys.waitUntilExt('SIM_IND', 120000)
function sys.waitUntilExt(id, ms)
    sys.subscribe(id, coroutine.running())
    local message = ms and {sys.wait(ms)} or {coroutine.yield()}
    sys.unsubscribe(id, coroutine.running())
    if message[1] ~= nil then return table.unpack(message) end
    return false
end
--- 创建一个任务线程,在模块最末行调用该函数并注册模块中的任务函数，main.lua导入该模块即可
-- @param fun 任务函数名，用于resume唤醒时调用
-- @param ... 任务函数fun的可变参数
-- @return co  返回该任务的线程号
-- @usage sys.taskInit(task1,'a','b')
function sys.taskInit(fun, ...)
    arg = { ... }
    local co = coroutine.create(fun)
    assert(coroutine.resume(co, table.unpack(arg)))
    return co
end
------------------------------------------ rtos消息回调处理部分 ------------------------------------------
--[[
函数名：cmpTable
功能  ：比较两个table的内容是否相同，注意：table中不能再包含table
参数  ：
t1：第一个table
t2：第二个table
返回值：相同返回true，否则false
]]
local function cmpTable(t1, t2)
if not t2 then return #t1 == 0 end
if #t1 == #t2 then
    for i = 1, #t1 do
        if table.unpack(t1, i, i) ~= table.unpack(t2, i, i) then
            return false
        end
    end
    return true
end
return false
end
--- 关闭定时器
-- @param val 值为number时，识别为定时器ID，值为回调函数时，需要传参数
-- @param ... val值为函数时，函数的可变参数
-- @return 无
-- @usage timerStop(1)
function sys.timerStop(val, ...)
    arg = { ... }
    -- val 为定时器ID
    if type(val) == 'number' then
        timerPool[val], para[val], loop[val] = nil
        _G['@this']:StopTimer(val)
    else
        for k, v in pairs(timerPool) do
            -- 回调函数相同
            if type(v) == 'table' and v.cb == val or v == val then
                -- 可变参数相同
                if cmpTable(arg, para[k]) then
                    _G['@this']:StopTimer(k)
                    timerPool[k], para[k], loop[val] = nil
                    break
                end
            end
        end
    end
end
--- 关闭同一回调函数的所有定时器
-- @param fnc 定时器回调函数
-- @return 无
-- @usage timerStopAll(cbFnc)
function sys.timerStopAll(fnc)
    for k, v in pairs(timerPool) do
        if type(v) == 'table' and v.cb == fnc or v == fnc then
            _G['@this']:StopTimer(k)
            timerPool[k], para[k], loop[k] = nil
        end
    end
end
--- 开启一个定时器
-- @param fnc 定时器回调函数
-- @number ms 整数，最大定时126322567毫秒
-- @param ... 可变参数 fnc的参数
-- @return number 定时器ID，如果失败，返回nil
function sys.timerStart(fnc, ms, ...)
    arg = { ... }
    --回调函数和时长检测
    assert(fnc ~= nil, 'sys.timerStart(first param) is nil !')
    assert(ms > 0, 'sys.timerStart(Second parameter) is <= zero !')
    -- 关闭完全相同的定时器
    if arg.n == 0 then
        sys.timerStop(fnc)
    else
        sys.timerStop(fnc, table.unpack(arg))
    end
    -- 为定时器申请ID，ID值 1-20 留给任务，20-30留给消息专用定时器
    while true do
        if msgId >= MSG_TIMER_ID_MAX then msgId = TASK_TIMER_ID_MAX end
        msgId = msgId + 1
        if timerPool[msgId] == nil then
            timerPool[msgId] = fnc
            break
        end
    end
    --调用底层接口启动定时器
    if _G['@this']:StartTimer(msgId, ms) ~= 1 then print('@this.StartTimer error') return end
    --如果存在可变参数，在定时器参数表中保存参数
    if arg.n ~= 0 then
        para[msgId] = arg
    end
    --返回定时器id
    return msgId
end
--- 开启一个循环定时器
-- @param fnc 定时器回调函数
-- @number ms 整数，最大定时126322567毫秒
-- @param ... 可变参数 fnc的参数
-- @return number 定时器ID，如果失败，返回nil
function sys.timerLoopStart(fnc, ms, ...)
    arg = { ... }
    local tid = sys.timerStart(fnc, ms, table.unpack(arg))
    if tid then loop[tid] = ms end
    return tid
end
--- 判断某个定时器是否处于开启状态
-- @param val 有两种形式
--一种是开启定时器时返回的定时器id，此形式时不需要再传入可变参数...就能唯一标记一个定时器
--另一种是开启定时器时的回调函数，此形式时必须再传入可变参数...才能唯一标记一个定时器
-- @param ... 可变参数
-- @return number 开启状态返回true，否则nil
function sys.timerIsActive(val, ...)
    arg = { ... }
    if type(val) == 'number' then
        return timerPool[val]
    else
        for k, v in pairs(timerPool) do
            if v == val then
                if cmpTable(arg, para[k]) then return true end
            end
        end
    end
end
------------------------------------------ LUA应用消息订阅/发布接口 ------------------------------------------
-- 订阅者列表
local subscribers = {}
--内部消息队列
local messageQueue = {}
--- 订阅消息
-- @param id 消息id
-- @param callback 消息回调处理
-- @usage subscribe('NET_STATUS_IND', callback)
function sys.subscribe(id, callback)
    if type(id) ~= 'string' or (type(callback) ~= 'function' and type(callback) ~= 'thread') then
        print('warning: sys.subscribe invalid parameter', id, callback)
        return
    end
    if not subscribers[id] then subscribers[id] = {} end
    subscribers[id][callback] = true
end
--- 取消订阅消息
-- @param id 消息id
-- @param callback 消息回调处理
-- @usage unsubscribe('NET_STATUS_IND', callback)
function sys.unsubscribe(id, callback)
    if type(id) ~= 'string' or (type(callback) ~= 'function' and type(callback) ~= 'thread') then
        print('warning: sys.unsubscribe invalid parameter', id, callback)
        return
    end
    if subscribers[id] then subscribers[id][callback] = nil end
end
-- 分发消息
local function dispatch()
    while true do
        if #messageQueue == 0 then
            break
        end
        local message = table.remove(messageQueue, 1)
        if subscribers[message[1]] then
            local cbs = {}
            for callback, _ in pairs(subscribers[message[1]]) do
                table.insert(cbs,callback)
            end
            for _,callback in ipairs(cbs) do
                if type(callback) == 'function' then
                    callback(table.unpack(message, 2, #message))
                elseif type(callback) == 'thread' then
                    local r,i = coroutine.resume(callback, table.unpack(message))
                    assert(r,i)
                end
            end
        end
    end
end
--- 发布内部消息，存储在内部消息队列中
-- @param ... 可变参数，用户自定义
-- @return 无
-- @usage publish('NET_STATUS_IND')
function sys.publish(...)
    arg = { ... }
    table.insert(messageQueue, arg)
    dispatch()
end
------------------------------------------ Luat 主调度框架  ------------------------------------------
function sys.tigger(param)
    if param < TASK_TIMER_ID_MAX then
        local taskId = timerPool[param]
        timerPool[param] = nil
        if taskTimerPool[taskId] == param then
            taskTimerPool[taskId] = nil
            local r,i = coroutine.resume(taskId)
            assert(r,i)
        end
    else
        local cb = timerPool[param]
        --如果不是循环定时器，从定时器id表中删除此定时器
        if not loop[param] then timerPool[param] = nil end
        if not cb then timerPool[param] = nil return end
        if para[param] ~= nil then
            cb(table.unpack(para[param]))
            if not loop[param] then para[param] = nil end
        else
            cb()
        end
        --如果是循环定时器，继续启动此定时器
        if loop[param] then _G['@this']:StartTimer(param, loop[param]) end
    end
end
--触发请求的回调
local tiggers = {}
--外部触发
function sys.tiggerCB(id,t,data)
    if id >= 0 and t == 'timer' then--定时器消息
        sys.tigger(id)
    elseif type(tiggers[t]) == 'function' then---其他外部触发
        tiggers[t](data)
    end
end
--注册回调触发函数
function sys.tiggerRegister(t,f)
    tiggers[t] = f
end
";
    }

    class LuaTaskData
    {
        public int id { get; set; }
        public string type { get; set; }
        public object data { get; set; }
    }
}
