 -- 串口文件传送脚本，按照如下协议分块进行传送，up是上位机即llcom软件，down是下位机。
 -- 状态机实现
 -- 本人用其向fpga烧录c代码，实测可用

-- A-> ask Y->yes N->no F->finish
-- up: A (请求发送数据)
-- down: Y (回复)
-- up: number(用字符串表示数字，结尾用X表示结束，代表发送的数据数目)
-- down: Y
-- up: data (数据)
-- down: Y
-- up: number
-- down: Y
-- up: data
-- down: Y
-- up: F
-- down: Y


--注册串口接收函数

apiSetCb("uart", function (data)
    log.info("uart receive",data)
    sys.publish("UART",data)--发布消息
end)

-- 0 start
-- 1 sending - 1
-- 2 sending - 2
-- 3 end
local state = 0
local file
local content
local bank_size = 4096

if state == 0 then
    apiSend("uart", "A")
    log.info("download", "start!!!")
end

--新建任务，等待接收到消息再继续运行
sys.taskInit(function()
    while true do
        --等待消息，超时1000ms
        local r,udata = sys.waitUntil("UART",126322567)
        local sendResult
        log.info("uart wait",r,udata)
        if r then
            if state == 0 and udata == "Y" then -- start
                    state = 1;
                    file = io.open("D:\\wk\\x\\easycpu\\code\\build\\easy.bin", "rb")
                    log.info("download", "openfile ", file)
            end
            if state == 1 and udata == "Y" then -- sending
                content = file:read(bank_size)
                if not content then
                    sendResult = apiSend("uart", "F")
                    state = 3
                    log.info("download", "finish!!!")
                else 
                    state = 2
                    local num = #content
                    sendResult = apiSend("uart", tonumber(num) .. "X")
                    log.info("download", "send bytes count of next, count: ", num)
                end
            elseif state == 2 and udata == "Y" then
                    state = 1
                    sendResult = apiSend("uart", content)
                    log.info("download", "send data")
            end

            --发送串口消息，并获取发送结果
            log.info("state ", state)
            log.info("uart send",sendResult)
            tag = 1
        end
    end
end)


--5s循环定时器
-- 软件模拟下位机 
--sys.timerLoopStart(sys.publish,5000, "UART", "Y")

return {}