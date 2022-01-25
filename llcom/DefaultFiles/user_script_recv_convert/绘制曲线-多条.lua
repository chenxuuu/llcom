--曲线支持10组，编号为0-9

--假设一次接收两个点
--假设接收格式为：
--点1的值,点2的值\r\n

--按换行符切开，防止粘包
local data = uartData:split("\r\n")

--每包都处理一遍
for i=1,#data do
    local temp = data[i]:split(",")--按逗号分开
    if #temp == 2 then--如果是俩数字就继续处理
        --两个数字都转成数值型
        local n1 = tonumber(temp[1])
        local n2 = tonumber(temp[2])
        if n1 and n2 then--如果两个数都有
            apiAddPoint(n1,0)--给曲线0加一个点
            apiAddPoint(n2,1)--给曲线1加一个点
        end
    end
end

--原样输出
return uartData

