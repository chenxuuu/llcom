using System.Collections.Concurrent;
using MoonSharp.Interpreter;

namespace llcom.LuaEnv;

/// <summary>
/// Lua virtual machine built on MoonSharp (pure C# Lua 5.2 interpreter).
/// Cross-platform: replaces XLua for Linux/macOS support.
/// Embeds the LuatOS coroutine scheduling framework (sys module).
/// </summary>
public class LuaEnv : IDisposable
{
    public Script lua;
    public event EventHandler<string>? ErrorEvent;
    public event EventHandler<string>? PrintEvent;
    public event EventHandler<bool>? StopEvent;

    private bool stop;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> timerPool = new();
    private readonly ConcurrentBag<LuaTaskData> toRun = new();
    private readonly object taskLock = new();
    private DynValue? triggerCB;

    private void Error(string msg) => ErrorEvent?.Invoke(lua, msg);

    public void Print(string msg) => PrintEvent?.Invoke(lua, msg);

    public void AddTrigger(string type, object? data) => AddTask(-1, type, data);

    private void AddTask(int id, string type, object? data)
    {
        if (stop) return;
        toRun.Add(new LuaTaskData { id = id, type = type, data = data });
        RunTask();
    }

    private void RunTask()
    {
        lock (taskLock)
            while (toRun.TryTake(out var task))
            {
                try
                {
                    triggerCB?.Function.Call(task.id, task.type, task.data ?? DynValue.Nil);
                }
                catch (Exception e)
                {
                    ErrorEvent?.Invoke(lua, e.Message);
                }
                if (stop) return;
            }
    }

    public int StartTimer(int id, int time)
    {
        var timerToken = new CancellationTokenSource();
        timerPool.AddOrUpdate(id, timerToken, (_, old) =>
        {
            try { old.Cancel(); } catch { }
            return timerToken;
        });

        var timer = new System.Timers.Timer(time) { AutoReset = false };
        timer.Elapsed += (_, _) =>
        {
            if (timerToken.IsCancellationRequested || stop) return;
            timerPool.TryRemove(id, out _);
            AddTask(id, "timer", null);
            timer.Dispose();
        };
        timer.Start();
        return 1;
    }

    public void StopTimer(int id)
    {
        if (timerPool.TryRemove(id, out var tc))
            try { tc.Cancel(); } catch { }
    }

    public DynValue DoString(string s)
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

    public DynValue DoFile(string f)
    {
        try
        {
            var s = File.ReadAllBytes(f);
            lock (taskLock) return lua.DoString(System.Text.Encoding.UTF8.GetString(s));
        }
        catch (Exception e)
        {
            ErrorEvent?.Invoke(lua, e.Message);
            throw new Exception(e.Message);
        }
    }

    /// <summary>Initialize Lua VM with LuatOS sys framework.</summary>
    public LuaEnv(object? input = null)
    {
        lua = new Script(CoreModules.Preset_Complete);
        lua.Options.DebugPrint = Print;

        if (input != null)
            lua.Globals["lua"] = input;

        lock (taskLock) lua.DoString(SysCode);

        var sysTable = lua.Globals.Get("sys").Table;
        triggerCB = sysTable.Get("tiggerCB");

        lua.Globals["@this"] = this;

        // Set up require paths
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

    public void Dispose()
    {
        lock (taskLock)
        {
            stop = true;
            lua = null!;
            foreach (var v in timerPool)
                try { v.Value.Cancel(); } catch { }
            timerPool.Clear();
            while (toRun.TryTake(out _)) ;
        }
        StopEvent?.Invoke(null, true);
    }

    // ---- Embedded LuatOS sys framework ----
    private static readonly string SysCode = @"
math.randomseed(tostring(os.time()):reverse():sub(1, 6))
sys = {}
local TASK_TIMER_ID_MAX = 0x1FFFFFFF
local MSG_TIMER_ID_MAX = 0x7FFFFFFF
local taskTimerId = 0
local msgId = TASK_TIMER_ID_MAX
local timerPool = {}
local taskTimerPool = {}
local para = {}
local loop = {}
function sys.wait(ms)
    assert(ms > 0, 'The wait time cannot be negative!')
    while true do
        if taskTimerId >= TASK_TIMER_ID_MAX - 1 then taskTimerId = 0
        else taskTimerId = taskTimerId + 1 end
        if taskTimerPool[taskTimerId] == nil then break end
    end
    local timerid = taskTimerId
    taskTimerPool[coroutine.running()] = timerid
    timerPool[timerid] = coroutine.running()
    if 1 ~= _G['@this']:StartTimer(timerid, ms) then print('sys.StartTimer error') return end
    local message = {coroutine.yield()}
    if #message ~= 0 then
        _G['@this']:StopTimer(timerid)
        taskTimerPool[coroutine.running()] = nil
        timerPool[timerid] = nil
        return table.unpack(message)
    end
end
function sys.waitUntil(id, ms)
    sys.subscribe(id, coroutine.running())
    local message = ms and {sys.wait(ms)} or {coroutine.yield()}
    sys.unsubscribe(id, coroutine.running())
    return message[1] ~= nil, table.unpack(message, 2, #message)
end
function sys.waitUntilExt(id, ms)
    sys.subscribe(id, coroutine.running())
    local message = ms and {sys.wait(ms)} or {coroutine.yield()}
    sys.unsubscribe(id, coroutine.running())
    if message[1] ~= nil then return table.unpack(message) end
    return false
end
function sys.taskInit(fun, ...)
    local arg = { ... }
    local co = coroutine.create(fun)
    assert(coroutine.resume(co, table.unpack(arg)))
    return co
end
local function cmpTable(t1, t2)
    if not t2 then return #t1 == 0 end
    if #t1 == #t2 then
        for i = 1, #t1 do
            if table.unpack(t1, i, i) ~= table.unpack(t2, i, i) then return false end
        end
        return true
    end
    return false
end
function sys.timerStop(val, ...)
    local arg = { ... }
    if type(val) == 'number' then
        timerPool[val], para[val], loop[val] = nil, nil, nil
        _G['@this']:StopTimer(val)
    else
        for k, v in pairs(timerPool) do
            if type(v) == 'table' and v.cb == val or v == val then
                if cmpTable(arg, para[k]) then
                    _G['@this']:StopTimer(k)
                    timerPool[k], para[k], loop[val] = nil, nil, nil
                    break
                end
            end
        end
    end
end
function sys.timerStopAll(fnc)
    for k, v in pairs(timerPool) do
        if type(v) == 'table' and v.cb == fnc or v == fnc then
            _G['@this']:StopTimer(k)
            timerPool[k], para[k], loop[k] = nil, nil, nil
        end
    end
end
function sys.timerStart(fnc, ms, ...)
    local arg = { ... }
    assert(fnc ~= nil, 'sys.timerStart(first param) is nil !')
    assert(ms > 0, 'sys.timerStart(Second parameter) is <= zero !')
    if arg.n == 0 then sys.timerStop(fnc)
    else sys.timerStop(fnc, table.unpack(arg)) end
    while true do
        if msgId >= MSG_TIMER_ID_MAX then msgId = TASK_TIMER_ID_MAX end
        msgId = msgId + 1
        if timerPool[msgId] == nil then
            timerPool[msgId] = fnc
            break
        end
    end
    if _G['@this']:StartTimer(msgId, ms) ~= 1 then print('@this.StartTimer error') return end
    if arg.n ~= 0 then para[msgId] = arg end
    return msgId
end
function sys.timerLoopStart(fnc, ms, ...)
    local arg = { ... }
    local tid = sys.timerStart(fnc, ms, table.unpack(arg))
    if tid then loop[tid] = ms end
    return tid
end
function sys.timerIsActive(val, ...)
    local arg = { ... }
    if type(val) == 'number' then return timerPool[val]
    else
        for k, v in pairs(timerPool) do
            if v == val then
                if cmpTable(arg, para[k]) then return true end
            end
        end
    end
end
local subscribers = {}
local messageQueue = {}
function sys.subscribe(id, callback)
    if type(id) ~= 'string' or (type(callback) ~= 'function' and type(callback) ~= 'thread') then
        print('warning: sys.subscribe invalid parameter', id, callback)
        return
    end
    if not subscribers[id] then subscribers[id] = {} end
    subscribers[id][callback] = true
end
function sys.unsubscribe(id, callback)
    if type(id) ~= 'string' or (type(callback) ~= 'function' and type(callback) ~= 'thread') then
        print('warning: sys.unsubscribe invalid parameter', id, callback)
        return
    end
    if subscribers[id] then subscribers[id][callback] = nil end
end
local function dispatch()
    while true do
        if #messageQueue == 0 then break end
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
function sys.publish(...)
    local arg = { ... }
    table.insert(messageQueue, arg)
    dispatch()
end
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
        if not loop[param] then timerPool[param] = nil end
        if not cb then timerPool[param] = nil return end
        if para[param] ~= nil then
            cb(table.unpack(para[param]))
            if not loop[param] then para[param] = nil end
        else
            cb()
        end
        if loop[param] then _G['@this']:StartTimer(param, loop[param]) end
    end
end
local tiggers = {}
function sys.tiggerCB(id,t,data)
    if id >= 0 and t == 'timer' then sys.tigger(id)
    elseif type(tiggers[t]) == 'function' then tiggers[t](data)
    end
end
function sys.tiggerRegister(t,f)
    tiggers[t] = f
end
";
}

internal class LuaTaskData
{
    public int id { get; set; }
    public string type { get; set; } = "";
    public object? data { get; set; }
}
