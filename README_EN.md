# LLCOM

![icon](/llcom/llcom.ico)

[![Build status](https://ci.appveyor.com/api/projects/status/telji5j8r0v5001c?svg=true)](https://ci.appveyor.com/project/chenxuuu/llcom)
[![MIT](https://img.shields.io/static/v1.svg?label=license&message=Apache+2&color=blue)](https://github.com/chenxuuu/llcom/blob/master/LICENSE)
[![code-size](https://img.shields.io/github/languages/code-size/chenxuuu/llcom.svg)](https://github.com/chenxuuu/llcom/archive/master.zip)

A serial port debugger tool, with lua script.

> this tool is only Chinese and English now, you can help me to translate, thanks!

## Download

Get it from Microsoft store:

<a href='//www.microsoft.com/store/apps/9PMPB0233S0S?cid=storebadge&ocid=badge'><img src='https://developer.microsoft.com/store/badges/images/English_get-it-from-MS.png' alt='English badge' style='width: 142px; height: 52px;'/></a>

Portable exe version: [GitHub](https://github.com/chenxuuu/llcom/releases/latest)

Appveyor snapshot version: [Appveyor Artifacts](https://ci.appveyor.com/project/chenxuuu/llcom/build/artifacts)

## Functions

- Basic functions of serial port debugger tools.
- The log is clear with two colors, display both HEX values and strings at same time.
- Auto save serial and lua script logs, with time stamp.
- Auto reconnect serial port after disconnected.
- Data you want to send can be processed with your own Lua scripts.
- Quick send bar on the right.
- Lua scripts can be run independently with timer and co-process task features.([Based on LUAT TASK](http://wiki.openluat.com/doc/luatFramework/))
- socket test server controler

![screenEN](/screenEN.png)
![screen2](/screen2.jpg)
![screen3](/screen3.png)

## features' exemples

### Use Lua script process data you want to send

1. end with "\r\n"

```lua
return uartData.."\r\n"
```

2. send HEX values

```lua
return uartData:fromHex()
```

this script can change `30313233` to `0123`.

3. another script example

```lua
json = require("JSON")
t = uartData:split(",")
return JSON:encode({
    key1 = t[1],
    key2 = t[2],
    key3 = t[3],
})
```

this script can change `a,b,c` to `{"key1":"a","key2":"b","key3":"c"}`.

**these scripts also work with Quick send bar**

### independent script auto process uart sand and receive

you can run your own Lua script on the right, such as llcom's example:

```lua
--register serial port receiver function
uartReceive = function (data)
    log.info("uartReceive",data)
    sys.publish("UART",data)--publish message
end

--create a task, wait for message
sys.taskInit(function()
    while true do
        local _,udata = sys.waitUntil("UART")--wait for message
        log.info("task waitUntil",udata)
        local sendResult = apiSendUartData("ok!")--send uart data
        log.info("uart send",sendResult)
    end
end)

--reate a task, sleep 1000ms and loop
sys.taskInit(function()
    while true do
        sys.wait(1000)--wait 1000ms
        log.info("task wait",os.time())
    end
end)

--1000ms loop timer
sys.timerLoopStart(log.info,1000,"timer test")
```

you alse can use `xlua` to use C# codes

```lua
request = CS.System.Net.WebRequest.Create("http://example.com")
request.ContentType = "text/html;charset=UTF-8";
request.Timeout = 5000;
request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36 Vivaldi/2.2.1388.37";

response = request:GetResponse():GetResponseStream()

myStreamReader = CS.System.IO.StreamReader(response, CS.System.Text.Encoding.UTF8);

print(myStreamReader:ReadToEnd())--get body

myStreamReader:Close()
response:Close()
```

you can make your debug automatic

## api document (in Chinese)

you can [click here](https://github.com/chenxuuu/llcom/blob/master/LuaApi.md)

## Known bugs and functions to be added

- [x] ~~bug: SerialPort The Requested Resource is in Use([.net's bug](https://github.com/dotnet/corefx/issues/39464))~~(fixed #2f26e68)

## Special Thanks

[![icon-resharper](/icon-resharper.svg)](https://www.jetbrains.com/?from=LLCOM)
