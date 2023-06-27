--在数据末尾加上校验码
--按需更改自己的校验方式

--校验码初始值
local checksum = 0
--从第几个字节开始
local start = 1

--开始算
--[[
更改符号可以变更校验方式：
与&
或|
异或~
校验和+
]]
for i=start,#uartData do
	--计算校验
	checksum = checksum ~ uartData:byte(i)
	--保证小于0x100
	checksum = checksum % 0x100
end

--返回结果
return uartData..string.char(checksum)

