# 接口列表

工具Lua环境为Lua 5.3版本，可直接使用原生自带功能。

## 发送处理脚本与Lua脚本运行区，可用接口的区别

发送处理脚本，不可用定时器/任务接口，也不可用log/print接口。

## 与合宙luat接口的差别

- 移植了大部分utils中的string接口

- 移植了全部log输出接口

- 移植了全部的定时器与任务相关接口

## C#层增加的接口(底层接口)

### apiSendUartData(string)

发送串口数据

* 参数

|传入值类型|释义|
|-|-|
|string|str，要发送的串口数据|

* 返回值

boolean，发送结果，成功为true

* 例子

```lua
local str = ("01020304"):fromHex()
local result = apiSendUartData(str)
```

### uartReceive回调

注意，本接口是用来回调的，如果使用Lua需要处理回调，请声明此函数为全局变量

* 例子

```lua
uartReceive = function (data)
    log.info("uartReceive",data)
end
```

### apiGetPath()

获取软件目录路径

* 参数

无

* 返回值

string，exe所在目录路径

* 例子

```lua
local path = apiGetPath()
```

### apiUtf8ToHex(str)

utf8转gbk编码的hex值

* 参数

|传入值类型|释义|
|-|-|
|string|str，要转换的utf8数据|

* 返回值

string，转换后的HEX字符串

* 例子

```lua
local str = apiUtf8ToHex("中文"):fromHex()
```

此功能仅用来兼容中文目录加载错误的问题，具体请见`head.lua`里的用法

## Lua层增加的接口

## string附加接口

模块功能：增加string可用的接口

### string.toHex(str, separator)

将Lua字符串转成HEX字符串，如"123abc"转为"313233616263"

* 参数

|传入值类型|释义|
|-|-|
|string|str 输入字符串|
|string|**可选参数，默认为`""`**，separator 输出的16进制字符串分隔符|

* 返回值

hexstring 16进制组成的串
len 输入的字符串长度

* 例子

```lua
string.toHex("\1\2\3") -> "010203" 3
string.toHex("123abc") -> "313233616263" 6
string.toHex("123abc"," ") -> "31 32 33 61 62 63 " 6
```

### string.fromHex(hex)

将HEX字符串转成Lua字符串，如"313233616263"转为"123abc", 函数里加入了过滤分隔符，可以过滤掉大部分分隔符（可参见正则表达式中\s和\p的范围）。

* 参数

|传入值类型|释义|
|-|-|
|string|hex,16进制组成的串|

* 返回值

charstring,字符组成的串
len,输出字符串的长度

* 例子

```lua
string.fromHex("010203")       ->  "\1\2\3"
string.fromHex("313233616263") ->  "123abc"
```

### string.toValue(str)

返回字符串tonumber的转义字符串(用来支持超过31位整数的转换)

* 参数

|传入值类型|释义|
|-|-|
|string|str 输入字符串|

* 返回值

str 转换后的lua 二进制字符串
len 转换了多少个字符

* 例子

```lua
string.toValue("123456") -> "\1\2\3\4\5\6"  6
string.toValue("123abc") -> "\1\2\3\a\b\c"  6
```

### string.utf8Len(str)

返回utf8编码字符串的长度

* 参数

|传入值类型|释义|
|-|-|
|string|str,utf8编码的字符串,支持中文|

* 返回值

number,返回字符串长度

* 例子

```lua
local cnt = string.utf8Len("中国a"),cnt == 3
```

### string.formatNumberThousands(num)

返回数字的千位符号格式

* 参数

|传入值类型|释义|
|-|-|
|number|num,数字|

* 返回值

string，千位符号的数字字符串

* 例子

```lua
loca s = string.formatNumberThousands(1000) ,s = "1,000"
```

### string.split(str, delimiter)

按照指定分隔符分割字符串

* 参数

|传入值类型|释义|
|-|-|
|string|str 输入字符串|
|string|delimiter 分隔符|

* 返回值

分割后的字符串列表

* 例子

```lua
"123,456,789":split(',') -> {'123','456','789'}
```

### string.urlEncode(str)

返回字符串的urlEncode编码

* 参数

|传入值类型|释义|
|-|-|
|string|str，要转换编码的字符串,支持UTF8编码中文|

* 返回值

str,urlEncode编码的字符串

* 例子

```lua
local str = string.urlEncode("####133") ,str == "%23%23%23%23133"
local str = string.urlEncode("中国2018") , str == "%e4%b8%ad%e5%9b%bd2018"
```

## log

模块功能：系统日志记录,分级别日志工具

### log.trace(tag, ...)

输出trace级别的日志

* 参数

|传入值类型|释义|
|-|-|
|param|tag   ，模块或功能名称，作为日志前缀|
|param|...   ，日志内容，可变参数|

* 返回值

nil

* 例子

```lua
trace('moduleA', 'log content')
```

---

### log.debug(tag, ...)

输出debug级别的日志

* 参数

|传入值类型|释义|
|-|-|
|param|tag   ，模块或功能名称，作为日志前缀|
|param|...   ，日志内容，可变参数|

* 返回值

nil

* 例子

```lua
debug('moduleA', 'log content')
```

---

### log.info(tag, ...)

输出info级别的日志

* 参数

|传入值类型|释义|
|-|-|
|param|tag   ，模块或功能名称，作为日志前缀|
|param|...   ，日志内容，可变参数|

* 返回值

nil

* 例子

```lua
info('moduleA', 'log content')
```

---

### log.warn(tag, ...)

输出warn级别的日志

* 参数

|传入值类型|释义|
|-|-|
|param|tag   ，模块或功能名称，作为日志前缀|
|param|...   ，日志内容，可变参数|

* 返回值

nil

* 例子

```lua
warn('moduleA', 'log content')
```

---

### log.error(tag, ...)

输出error级别的日志

* 参数

|传入值类型|释义|
|-|-|
|param|tag   ，模块或功能名称，作为日志前缀|
|param|...   ，日志内容，可变参数|

* 返回值

nil

* 例子

```lua
error('moduleA', 'log content')
```

---

### log.fatal(tag, ...)

输出fatal级别的日志

* 参数

|传入值类型|释义|
|-|-|
|param|tag   ，模块或功能名称，作为日志前缀|
|param|...   ，日志内容，可变参数|

* 返回值

nil

* 例子

```lua
fatal('moduleA', 'log content')
```

---

## sys

模块功能：Luat协程调度框架

### sys.wait(ms)

Task任务延时函数，只能用于任务函数中

* 参数

|传入值类型|释义|
|-|-|
|number|ms  整数，最大等待126322567毫秒|

* 返回值

定时结束返回nil,被其他线程唤起返回调用线程传入的参数

* 例子

```lua
sys.wait(30)
```

---

### sys.waitUntil(id, ms)

Task任务的条件等待函数（包括事件消息和定时器消息等条件），只能用于任务函数中。

* 参数

|传入值类型|释义|
|-|-|
|param|id 消息ID|
|number|ms 等待超时时间，单位ms，最大等待126322567毫秒|

* 返回值

result 接收到消息返回true，超时返回false
data 接收到消息返回消息参数

* 例子

```lua
result, data = sys.waitUntil("SIM_IND", 120000)
```

---

### sys.waitUntilExt(id, ms)

Task任务的条件等待函数扩展（包括事件消息和定时器消息等条件），只能用于任务函数中。

* 参数

|传入值类型|释义|
|-|-|
|param|id 消息ID|
|number|ms 等待超时时间，单位ms，最大等待126322567毫秒|

* 返回值

message 接收到消息返回message，超时返回false
data 接收到消息返回消息参数

* 例子

```lua
result, data = sys.waitUntilExt("SIM_IND", 120000)
```

---

### sys.taskInit(fun, ...)

创建一个任务线程,在模块最末行调用该函数并注册模块中的任务函数，main.lua导入该模块即可

* 参数

|传入值类型|释义|
|-|-|
|param|fun 任务函数名，用于resume唤醒时调用|
|param|... 任务函数fun的可变参数|

* 返回值

co  返回该任务的线程号

* 例子

```lua
sys.taskInit(task1,'a','b')
```

---

### sys.timerStop(val, ...)

关闭定时器

* 参数

|传入值类型|释义|
|-|-|
|param|val 值为number时，识别为定时器ID，值为回调函数时，需要传参数|
|param|... val值为函数时，函数的可变参数|

* 返回值

无

* 例子

```lua
timerStop(1)
```

---

### sys.timerStopAll(fnc)

关闭同一回调函数的所有定时器

* 参数

|传入值类型|释义|
|-|-|
|param|fnc 定时器回调函数|

* 返回值

无

* 例子

```lua
timerStopAll(cbFnc)
```

---

### sys.timerStart(fnc, ms, ...)

开启一个定时器

* 参数

|传入值类型|释义|
|-|-|
|param|fnc 定时器回调函数|
|number|ms 整数，最大定时126322567毫秒|
|param|... 可变参数 fnc的参数|

* 返回值

number 定时器ID，如果失败，返回nil

* 例子

无

---

### sys.timerLoopStart(fnc, ms, ...)

开启一个循环定时器

* 参数

|传入值类型|释义|
|-|-|
|param|fnc 定时器回调函数|
|number|ms 整数，最大定时126322567毫秒|
|param|... 可变参数 fnc的参数|

* 返回值

number 定时器ID，如果失败，返回nil

* 例子

无

---

### sys.timerIsActive(val, ...)

判断某个定时器是否处于开启状态

* 参数

|传入值类型|释义|
|-|-|
|param|val 有两种形式<br>一种是开启定时器时返回的定时器id，此形式时不需要再传入可变参数...就能唯一标记一个定时器<br>另一种是开启定时器时的回调函数，此形式时必须再传入可变参数...才能唯一标记一个定时器|
|param|... 可变参数|

* 返回值

number 开启状态返回true，否则nil

* 例子

无

---

### sys.subscribe(id, callback)

订阅消息

* 参数

|传入值类型|释义|
|-|-|
|param|id 消息id|
|param|callback 消息回调处理|

* 返回值

无

* 例子

```lua
subscribe("NET_STATUS_IND", callback)
```

---

### sys.unsubscribe(id, callback)

取消订阅消息

* 参数

|传入值类型|释义|
|-|-|
|param|id 消息id|
|param|callback 消息回调处理|

* 返回值

无

* 例子

```lua
unsubscribe("NET_STATUS_IND", callback)
```

---

### sys.publish(...)

发布内部消息，存储在内部消息队列中

* 参数

|传入值类型|释义|
|-|-|
|param|... 可变参数，用户自定义|

* 返回值

无

* 例子

```lua
publish("NET_STATUS_IND")
```

