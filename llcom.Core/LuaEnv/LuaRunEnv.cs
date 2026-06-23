using System.Collections.Concurrent;
using MoonSharp.Interpreter;

namespace llcom.LuaEnv;

/// <summary>
/// Lua script execution environment for user scripts.
/// Manages the lifecycle of a user Lua VM instance with channel dispatching.
/// </summary>
public static class LuaRunEnv
{
    public static event EventHandler? LuaRunError;

    private static Script? lua;
    private static CancellationTokenSource? tokenSource;
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> pool = new();
    private static readonly ConcurrentBag<LuaPool> toRun = new();
    private static DynValue? triggerCB;

    public static bool IsRunning { get; private set; }
    public static bool CanRun { get; private set; }

    private static void AddTrigger(int id, string type = "timer", byte[]? data = null)
    {
        if (!IsRunning) return;
        toRun.Add(new LuaPool { id = id, type = type, data = data });
        RunTrigger();
    }

    /// <summary>Execute a Lua code snippet at runtime.</summary>
    public static void RunCommand(string l)
    {
        AddTrigger(-1, "cmd", System.Text.Encoding.UTF8.GetBytes(l));
    }

    /// <summary>Dispatch received channel data to Lua.</summary>
    public static void ChannelReceived(string channel, object? data)
    {
        if (!IsRunning) return;
        toRun.Add(new LuaPool { id = -1, type = channel, data = data });
        RunTrigger();
    }

    private static void RunTrigger()
    {
        if (!CanRun || lua == null) return;
        lock (lua)
        {
            try
            {
                while (toRun.TryTake(out var temp))
                {
                    if (tokenSource?.IsCancellationRequested == true) return;
                    try
                    {
                        triggerCB?.Function.Call(temp.id, temp.type, temp.data ?? DynValue.Nil);
                    }
                    catch (Exception le)
                    {
                        LuaApis.PrintLog("Callback error:\r\n" + le);
                    }
                    if (tokenSource?.IsCancellationRequested == true) return;
                }
            }
            catch (Exception ex)
            {
                StopLua(ex.ToString());
            }
        }
    }

    public static int StartTimer(int id, int time)
    {
        var timerToken = new CancellationTokenSource();
        if (pool.TryRemove(id, out var old))
            try { old.Cancel(); } catch { }
        pool.TryAdd(id, timerToken);

        var timer = new System.Timers.Timer(time) { AutoReset = false };
        timer.Elapsed += (_, _) =>
        {
            if (timerToken.IsCancellationRequested || !IsRunning) return;
            pool.TryRemove(id, out _);
            AddTrigger(id);
            timer.Dispose();
        };
        timer.Start();
        return 1;
    }

    public static void StopTimer(int id)
    {
        if (pool.TryRemove(id, out var tc))
            try { tc.Cancel(); } catch { }
    }

    public static void StopLua(string ex)
    {
        LuaRunError?.Invoke(null, EventArgs.Empty);
        if (!string.IsNullOrEmpty(ex))
            LuaApis.PrintLog("Lua error:\r\n" + ex);
        else
            LuaApis.PrintLog("Lua stopped");

        foreach (var v in pool)
            try { v.Value.Cancel(); } catch { }
        IsRunning = false;
        tokenSource?.Cancel();
        pool.Clear();
        lua = null;
    }

    public static void New(string file)
    {
        CanRun = false;
        IsRunning = true;
        tokenSource?.Dispose();
        tokenSource = new CancellationTokenSource();

        var fullPath = llcom.Tools.PlatformHelper.ProfilePath + file;
        if (!File.Exists(fullPath)) return;

        Task.Run(() =>
        {
            while (!CanRun)
                Task.Delay(100).Wait();

            try
            {
                lua = new Script(CoreModules.Preset_Complete);
                lock (lua)
                {
                    lua.Globals["runType"] = "script";
                    LuaLoader.Initial(lua);
                    triggerCB = lua.Globals.Get("tiggerCB");
                    var requirePath = file.Replace("/", ".").Substring(0, file.Length - 4);
                    lua.DoString($"require '{requirePath}'");
                }
            }
            catch (Exception ex)
            {
                StopLua(ex.ToString());
            }
        }, tokenSource.Token);
    }
}

internal class LuaPool
{
    public int id { get; set; }
    public string type { get; set; } = "";
    public object? data { get; set; }
}
