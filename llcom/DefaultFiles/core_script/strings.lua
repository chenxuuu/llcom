--- 模块功能：常用工具类接口
-- @module utils
-- @author openLuat
-- @license MIT
-- @copyright openLuat
-- @release 2017.10.19

--- 将Lua字符串转成HEX字符串，如"123abc"转为"313233616263"
-- @string str 输入字符串
-- @string[opt=""] separator 输出的16进制字符串分隔符
-- @return hexstring 16进制组成的串
-- @return len 输入的字符串长度
-- @usage
-- string.toHex("\1\2\3") -> "010203" 3
-- string.toHex("123abc") -> "313233616263" 6
-- string.toHex("123abc"," ") -> "31 32 33 61 62 63 " 6
function string.toHex(str, separator)
    return str:gsub('.', function(c)
        return string.format("%02X" .. (separator or ""), string.byte(c))
    end)
end
--- 将HEX字符串转成Lua字符串，如"313233616263"转为"123abc", 函数里加入了过滤分隔符，可以过滤掉大部分分隔符（可参见正则表达式中\s和\p的范围）。
-- @string hex,16进制组成的串
-- @return charstring,字符组成的串
-- @return len,输出字符串的长度
-- @usage
-- string.fromHex("010203")       ->  "\1\2\3"
-- string.fromHex("313233616263:) ->  "123abc"
function string.fromHex(hex)
    --滤掉分隔符
    local hex = hex:gsub("[%s%p]", ""):upper()
    return hex:gsub("%x%x", function(c)
        return string.char(tonumber(c, 16))
    end)
end

-- 返回字符串tonumber的转义字符串(用来支持超过31位整数的转换)
-- @string str 输入字符串
-- @return str 转换后的lua 二进制字符串
-- @return len 转换了多少个字符
-- @usage
-- string.toValue("123456") -> "\1\2\3\4\5\6"  6
-- string.toValue("123abc") -> "\1\2\3\a\b\c"  6
function string.toValue(str)
    return string.fromHex(str:gsub("%x", "0%1"))
end

--- 返回utf8编码字符串的长度
-- @string str,utf8编码的字符串,支持中文
-- @return number,返回字符串长度
-- @usage local cnt = string.utf8Len("中国"),str = 2
function string.utf8Len(str)
    local len = #str
    local left = len
    local cnt = 0
    local arr = {0, 0xc0, 0xe0, 0xf0, 0xf8, 0xfc}
    while left ~= 0 do
        local tmp = string.byte(str, -left)
        local i = #arr
        while arr[i] do
            if tmp >= arr[i] then
                left = left - i
                break
            end
            i = i - 1
        end
        cnt = cnt + 1
    end
    return cnt
end
--- 返回数字的千位符号格式
-- @number num,数字
-- @return string，千位符号的数字字符串
-- @usage loca s = string.formatNumberThousands(1000) ,s = "1,000"
function string.formatNumberThousands(num)
    local k, formatted
    formatted = tostring(tonumber(num))
    while true do
        formatted, k = string.gsub(formatted, "^(-?%d+)(%d%d%d)", '%1,%2')
        if k == 0 then break end
    end
    return formatted
end

--- 按照指定分隔符分割字符串
-- @string str 输入字符串
-- @string delimiter 分隔符
-- @return 分割后的字符串列表
-- @usage "123,456,789":split(',') -> {'123','456','789'}
function string.split(str, delimiter)
    local strlist, tmp = {}, string.byte(delimiter)
    if delimiter == "" then
        for i = 1, #str do strlist[i] = str:sub(i, i) end
    else
        for substr in string.gmatch(str .. delimiter, "(.-)" .. (((tmp > 96 and tmp < 123) or (tmp > 64 and tmp < 91) or (tmp > 47 and tmp < 58)) and delimiter or "%" .. delimiter)) do
            table.insert(strlist, substr)
        end
    end
    return strlist
end

--- 返回utf8编码字符串的长度
-- @string str,utf8编码的字符串,支持中文
-- @return number,返回字符串长度
-- @usage local cnt = string.utf8Len("中国"),str = 2
function string.utf8Len(str)
    local len = #str
    local left = len
    local cnt = 0
    local arr = {0, 0xc0, 0xe0, 0xf0, 0xf8, 0xfc}
    while left ~= 0 do
        local tmp = string.byte(str, -left)
        local i = #arr
        while arr[i] do
            if tmp >= arr[i] then
                left = left - i
                break
            end
            i = i - 1
        end
        cnt = cnt + 1
    end
    return cnt
end
-- 将一个字符转为urlEncode编码
local function urlEncodeChar(c)
    return "%" .. string.format("%02X", string.byte(c))
end
--- 返回字符串的urlEncode编码
-- @string str，要转换编码的字符串
-- @return str,urlEncode编码的字符串
-- @usage string.urlEncode("####133")
function string.urlEncode(str)
    return string.gsub(string.gsub(string.gsub(tostring(str), "\n", "\r\n"), "([^%w%.%- ])", urlEncodeChar), " ", "+")
end
