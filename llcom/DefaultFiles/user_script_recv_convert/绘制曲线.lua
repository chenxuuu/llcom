--假设接收格式为：
--值\r\n

--按换行符切开，防止粘包
local data = uartData:split("\r\n")

--每包都处理一遍
for i=1,#data do
    local temp = tonumber(data[i])--转成数值型
    if temp then--如果有数
        apiAddPoint(temp,0)--给曲线0加一个点
    end
end

--原样输出
return uartData

