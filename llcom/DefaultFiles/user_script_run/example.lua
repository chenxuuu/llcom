--由于软件有自动保存
--所以请勿在开启本软件的同时
--用其他编辑器编辑此处打开了的脚本
--以免被覆盖掉
--建议在此处require你在改的脚本

--注册串口接收函数
uartReceive = function (data)
    log.info("uart receive",data)
    sys.publish("UART",data)--发布消息
end

--新建任务，等待接收到消息再继续运行
sys.taskInit(function()
    while true do
        --等待消息，超时1000ms
        local r,udata = sys.waitUntil("UART",1000)
        log.info("uart wait",r,udata)
        if r then
            --发送串口消息，并获取发送结果
            local sendResult = apiSendUartData("ok!")
            log.info("uart send",sendResult)
        end
    end
end)

--新建任务，每休眠1000ms继续一次
sys.taskInit(function()
    while true do
        sys.wait(1000)--等待1000ms
        log.info("task wait",os.time())
    end
end)

--1000ms循环定时器
sys.timerLoopStart(log.info,1000,"timer test")
