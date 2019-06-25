--提前运行的脚本
--用于提前声明某些要用到的函数

--加强随机数随机性
math.randomseed(tostring(os.time()):reverse():sub(1, 6))

--防止跑死循环，超时设置秒数自动结束，-1表示禁用
runMaxSeconds = runType == "send" and 3 or -1
local start = os.time()
function trace (event, line)
    if runMaxSeconds > 0 and os.time() - start >=runMaxSeconds then
        error("代码运行超时")
    end
end
debug.sethook(trace, "l")

--加上需要require的路径
local rootPath = apiUtf8ToHex(apiGetPath())
rootPath = rootPath:gsub("[%s%p]", ""):upper()
rootPath = rootPath:gsub("%x%x", function(c)
                                    return string.char(tonumber(c, 16))
                                end)
package.path = package.path..
";"..rootPath.."core_script/?.lua"..
";"..rootPath.."user_script_run/requires/?.lua"

--加载字符串工具包
require("strings")

--重载几个可能影响中文目录的函数
local oldrequire = require
require = function (s)
    local s = apiUtf8ToHex(s):fromHex()
    return oldrequire(s)
end
local oldloadfile = loadfile
loadfile = function (s)
    local s = apiUtf8ToHex(s):fromHex()
    return oldloadfile(s)
end

--下面的代码一次性的处理函数用不着
if runType == "send" then return end

--重写print函数
function print(...)
    arg = { ... }
    local logAll = {}
    for i=1,#arg do
        if type(arg[i]) ~= "string" then
            table.insert(logAll, tostring(arg[i]))
        else
            local str = apiUtf8ToHex(arg[i]):fromHex()
            table.insert(logAll, str)
        end
    end
    apiPrintLog(table.concat(logAll, "\t"))
end

log = require("log")
sys = require("sys")

--串口数据接收接口
local uartCB = function (d)
    if uartReceive then uartReceive(d:fromHex()) end
end

--重写串口数据发送接口
local oldapiSendUartData = apiSendUartData
apiSendUartData = function (s)
    local str = s:toHex()
    return oldapiSendUartData(str)
end

--协程外部触发
tiggerCB = function (id,type,data)
    --log.debug("tigger",id,type,data:toHex())
    if type == "uartRev" then--串口消息
        uartCB(data)
    elseif type == "cmd" then
        local result, info = pcall(function ()
            load(data)()
        end)
        if result then
            log.info("console","run success")
        else
            log.info("console","run failed\r\n"..tostring(info))
        end
    elseif id >= 0 then--定时器消息
        sys.tigger(id)
    end
end



