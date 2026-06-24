using System.Collections;
using MoonSharp.Interpreter;

namespace llcom.LuaEnv;

/// <summary>
/// Lua script loader for the cross-platform Lua environment.
/// Initializes Lua globals and provides the send/recv script execution pipeline.
/// </summary>
public static class LuaLoader
{
    /// <summary>Initialize a Lua VM with API bindings.</summary>
    public static void Initial(Script lua, string t = "script")
    {
        // Register C# API methods as Lua globals
        lua.Globals["apiUtf8ToHex"] = (Func<string, string>)LuaApis.Utf8ToAsciiHex;
        lua.Globals["apiAscii2Utf8"] = (Func<byte[], byte[]>)LuaApis.Ascii2Utf8;
        lua.Globals["apiGetPath"] = (Func<string>)LuaApis.GetPath;
        lua.Globals["apiPrintLog"] = (Action<string>)LuaApis.PrintLog;
        lua.Globals["apiQuickSendList"] = (Func<int, string>)LuaApis.QuickSendList;

        // InputBox: MoonSharp doesn't support 'out' params well, use Tuple return
        lua.Globals["apiInputBox"] = (Func<string, string, string, (bool, string)>)InputBoxWrapper;

        // apiSend: use table param
        lua.Globals["apiSend"] = (Func<string, byte[], Table, bool>)SendWrapper;

        if (t != "send")
        {
            lua.Globals["apiStartTimer"] = (Func<int, int, int>)LuaRunEnv.StartTimer;
            lua.Globals["apiStopTimer"] = (Action<int>)LuaRunEnv.StopTimer;
        }

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

        // Load head.lua
        lua.DoString("require 'core_script.head'");

        if (t == "send")
        {
            lua.DoString(@"
local rootPath = apiUtf8ToHex(apiGetPath()):fromHex()
local script = {}
_G['!once!'] = function()
    runLimitStart(3)
    if not script[_G['!file!']] then
        script[_G['!file!']] = load(CS.System.IO.File.ReadAllText(_G['!file!']))
    end
    local result = script[_G['!file!']]()
    runLimitStop()
    return result
end
");
        }
    }

    private static (bool, string) InputBoxWrapper(string prompt, string defaultInput, string title)
    {
        var result = LuaApis.InputBox(prompt, defaultInput, title);
        return (result, LuaApis.LastInputResult ?? defaultInput);
    }

    private static bool SendWrapper(string channel, byte[] data, Table table)
    {
        var dict = new Dictionary<string, object?>();
        if (table != null)
        {
            foreach (var pair in table.Pairs)
            {
                if (pair.Key.Type == DataType.String)
                    dict[pair.Key.String] = pair.Value.ToObject();
            }
        }
        return LuaApis.Send(channel, data, dict);
    }

    #region Send script runner

    private static Script? luaRunner;

    public static byte[] Run(string file, ArrayList? args = null, string path = "user_script_send_convert/")
    {
        var fullPath = llcom.Tools.PlatformHelper.ProfilePath + path + file;
        if (!File.Exists(fullPath))
            return Array.Empty<byte>();

        if (luaRunner == null)
        {
            luaRunner = new Script(CoreModules.Preset_Complete);
            lock (luaRunner)
            {
                luaRunner.Globals["runType"] = "send";
                Initial(luaRunner, "send");
            }
        }
        lock (luaRunner)
        {
            luaRunner.Globals["!file!"] = fullPath;
            while (luaRunner.Globals.Get("!file!").String != fullPath)
                luaRunner.Globals["!file!"] = fullPath;

            if (args != null)
                for (int i = 0; i < args.Count; i += 2)
                    luaRunner.Globals[(string)args[i]!] = args[i + 1];

            try
            {
                var f = luaRunner.Globals.Get("!once!");
                if (f.Type == DataType.Function)
                {
                    var result = f.Function.Call();
                    if (result.Type == DataType.UserData && result.UserData.Object is byte[] bytes)
                        return bytes;
                }
                return Array.Empty<byte>();
            }
            catch (Exception)
            {
                luaRunner = null;
                throw;
            }
        }
    }

    public static void ClearRun()
    {
        luaRunner = null;
    }

    #endregion
}
