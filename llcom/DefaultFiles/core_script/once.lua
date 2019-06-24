--只运行一次的代码
local rootPath = apiUtf8ToHex(apiGetPath()):fromHex()
local file = apiUtf8ToHex(file):fromHex()
uartData = uartData:fromHex()
return dofile(rootPath.."user_script_send_convert/"..file):toHex()


