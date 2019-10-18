--调用c#接口进行tcp调试
ipStr = "180.97.80.55"
port = 12415
sendStr = "hello server"

sys.taskInit(function ()
	tcpClient = CS.System.Net.Sockets.TcpClient(ipStr, port)
	stream = tcpClient:GetStream()
	while true do
		-- Send the message to the connected TcpServer.
		stream:Write(sendStr, 0, #sendStr)

		print("Sent:", sendStr)

		sys.wait(100)
		print("start recv", stream.CanRead)

		local revT = {}
		while stream.DataAvailable do
			table.insert(revT,string.char(stream:ReadByte()))
		end
		local rev = table.concat(revT)
		print("receive",rev,#rev)
	end
	-- Close everything.
	stream:Close()
	tcpClient:Close()
end)
