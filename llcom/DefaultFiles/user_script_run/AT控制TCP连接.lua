--适用于合宙Air202/Air720 AT TCP连接测试
local server,ip = "180.97.80.55","12415"

local torev = "OK"
--注册串口接收函数
uartReceive = function (data)
    log.info("uart receive")
    if data:find(torev) then
        sys.publish("UART",data)--发布消息
    end
end

--循环跑某命令，直到成功
function rollRun(cmd,receive,timeout)
    if not timeout then timeout = 1000 end
    while true do
        torev = receive
        log.info("uart send",apiSendUartData(cmd.."\r\n"))
        local r,d = sys.waitUntil("UART",timeout)
        if r then break end
    end
end

sys.taskInit(function ()
    log.info("check start")
    rollRun("AT","OK")

    log.info("check version")
    rollRun("ATI","OK")

    log.info("check sim card")
    rollRun("AT+CPIN?","+CPIN: READY")

    log.info("check network")
    rollRun("AT+CGATT?","+CGATT: 1")


    log.info("set apn")
    rollRun('AT+CSTT="CMIOT"',"OK",5000)

    log.info("AT+CIICR")
    rollRun("AT+CIICR","OK",5000)

    log.info("check ip")
    rollRun("AT+CIFSR","%.",1000)

    log.info("connect server")
    rollRun([[AT+CIPSTART="TCP","]]..server..[[",]]..ip,"CONNECT OK",5000)

    while true do
        log.info("start send data")
        rollRun("AT+CIPSEND=10",">",1000)

        log.info("data content")
        rollRun("1234567890","1234567890",1000)

        sys.wait(5000)
    end
end)



