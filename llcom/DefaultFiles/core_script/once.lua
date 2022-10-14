--只运行一次的代码
local rootPath = apiUtf8ToHex(apiGetPath()):fromHex()

return function ()
    runLimitStart(3)
    local file = apiUtf8ToHex(file):fromHex()
    local result = dofile(rootPath..file):toHex()
    runLimitStop()
    return result
end


