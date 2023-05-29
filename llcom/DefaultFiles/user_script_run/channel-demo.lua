--[[
通用消息通道示例代码
该功能拓展了lua脚本的控制范围
可以更加灵活地进行自动化测试
]]

-- uart，对应软件自身的串口功能
apiSetCb("uart",function (data)
    log.info("uart received",data)
end)
local sendResult = apiSend("uart","ok!")

-- mqtt，对应 MQTT 选项卡
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

-- tcp-server，对应 本机TCP服务端 选项卡
apiSetCb("tcp-server",function (data)
  log.info(
    "tcp-server received",
    data.from,
    data.data)
end)
local sendResult = apiSend("tcp-server","broadcast message!")

-- socket-client，对应 socket客户端 选项卡
apiSetCb("socket-client",function (data)
  log.info("socket-client received", data)
end)
local sendResult = apiSend("socket-client","send message by lua!")

-- netlab，对应 socket公共服务端 选项卡
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

-- winusb，对应 winusb 选项卡
apiSetCb("winusb",function (data)
  log.info("winusb received", data)
end)
local sendResult = apiSend("winusb",string.char(0,4))
