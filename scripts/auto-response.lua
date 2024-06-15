--注册串口接收函数
uartReceive = function (data)
    sys.publish("UART",data)--发布消息
end

--自动回复
sys.taskInit(function()
    while true do
        --等待消息，超时1000ms
        local r,udata = sys.waitUntil("UART",1000)
        if r then
        	local receiveHex=string.toHex(udata)
        	log.info("收到:",receiveHex)
        	
        	-- 由于没有API获取快捷发送列表数量,这里写100或者更大数字,通过判断跳出
		    for i = 1, 100 do
		        local data = apiQuickSendList(i, false)
		        if data and #data>0 then					
					if data:sub(1,1) == "H" then
						if data:find(receiveHex) then
							local tmpData = data:gsub('H'..receiveHex..'~', '')
							log.info("发送", apiSendUartData(tmpData:fromHex()), tmpData)
							break
						end
					end
	            else
		            log.info("Current Complet")
		            break
		        end
		    end
        end
    end
end)
