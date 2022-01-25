--曲线支持10组，编号为0-9

--假设一次接收三个点
--假设接收格式为（小端数据）：
--【uint8_t数据】【int32_t的数据】【float数据】0x0A
-- 按字节分就是：1字节数据 4字节数据 4字节数据 1字节包尾

--按换行符切开，防止粘包
local data = uartData:split(string.char(0x0A))

--每包都处理一遍
for i=1,#data do
    --如果数据长度对的上
    if #data[i] == (1+4+4) then
        --解包成能用的数，具体含义参考https://cloudwu.github.io/lua53doc/manual.html#6.4.2
        local u8,i32,f32 = string.unpack("<Blf",data[i])
        apiAddPoint(u8,0)--给曲线0加一个点
        apiAddPoint(i32,1)--给曲线1加一个点
        apiAddPoint(f32,2)--给曲线2加一个点
    end
end

--原样输出
return uartData

