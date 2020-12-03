--树莓派登陆脚本，需开启树莓派串口才能使用

local USERNAME = "pi" --这里修改为你的树莓派用户名
local PASSWORD = "raspiberry" ----这里修改为你的树莓派密码

local UART_MSG_ID = "PI-UART"

--串口接收回调
uartReceive = function (str)
    --log.info("uartRec: ",str)
    sys.publish(UART_MSG_ID,str)--发布消息
end

--根据串口接收来发送的函数
function uartWaitSend(waitString,sendString,timeoutMs)
	--超时等待
    local r,str = sys.waitUntil(UART_MSG_ID,timeoutMs)
    log.info("uartWait: ",r,str)
    if r then
    	--字符串匹配
    	if string.find(str,waitString) then
    		--发送
        	local sendResult = apiSendUartData(sendString)
        	if sendResult then
        		log.info("uartSend: ",sendString)
        		return true, str
        	else
        		log.info("uartSend failed!!!")
        	end
    	end
    end
    
    return false,str
end

--登陆任务
sys.taskInit(function()
    local r,s = uartWaitSend("login",USERNAME.."\r\n",2000)
    r,s = uartWaitSend("Password",PASSWORD.."\r\n",2000)
    --r,s = uartWaitSend("密码",PASSWORD.."\r\n",2000)
end)

