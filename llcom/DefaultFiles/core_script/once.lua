--只运行一次的代码
local rootPath = apiUtf8ToHex(apiGetPath()):fromHex()

return function ()
    runLimitStart(3)
    local file = apiUtf8ToHex(file):fromHex()
    uartData = uartData:fromHex()
    local result = dofile(rootPath.."user_script_send_convert/"..file):toHex()
    runLimitStop()
    return result
end


