--[[
通用消息通道示例代码
该功能拓展了lua脚本的控制范围
可以更加灵活地进行自动化测试
]]

-- 串口：uart
apiSetCb("uart",function (data)
    log.info("uart received",data)
end)
local sendResult = apiSend("uart","ok!")

-- mqtt
apiSetCb("mqtt",function (data)
  log.info(
    "mqtt received",
    data.topic,
    data.payload,
    data.qos)
end)
local sendResult = apiSend("mqtt",nil,
{
  topic   = "test",
  payload = "test",
  qos     = 0
})

-- tcp-server
apiSetCb("tcp-server",function (data)
  log.info(
    "tcp-server received",
    data.from,
    data.data)
end)
local sendResult = apiSend("tcp-server","broadcast message!")

-- socket-client
apiSetCb("socket-client",function (data)
  log.info("socket-client received", data)
end)
local sendResult = apiSend("socket-client","send message by lua!")

-- netlab
apiSetCb("netlab",function (data)
  log.info(
    "netlab received",
    data.client,
    data.data)
end)
local sendResult = apiSend("netlab",nil,
{
  client = "aioSession--718957913",
  data   = "test data~"
})