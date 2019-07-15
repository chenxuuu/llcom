--自动处理GPS NMEA数据
--不需要输入*及其后面的数据
--例如输入：$PGKC030,1,1
--会自动处理成：$PGKC030,1,1*2C<回车><换行>
local check = uartData:byte(2)
for i=3,uartData:len() do
	check = check ~ uartData:byte(i)
end

return uartData.."*"..
string.char(check%256):toHex().."\r\n"

