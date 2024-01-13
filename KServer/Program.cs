using KServer.DataStructs;
using KServer.Server;
using KServer.Attributes;
using ProtoBuf;


TCPServer tcpserver = new TCPServer();

tcpserver.Host = "0.0.0.0";
tcpserver.Port = 20060;
tcpserver.Start();

UDPServer udpserver = new UDPServer();
tcpserver.Host = "0.0.0.0";
tcpserver.Port = 20070;
tcpserver.Start();

while (true)
{
	string cmd = Console.ReadLine();
	if (cmd == "stop")
	{
		tcpserver.Dispose();
		udpserver.Dispose();
		break;
	}
}


[DataDeserializer("TransformData")]
object TransformDataDeserializer(byte[] data)
{
	return Serializer.Deserialize<TransformData>(new MemoryStream(data));
}

[DataAction("TransformData")]
void TransformDataAction(object obj, Connection conn)
{
	TransformData tdata = (TransformData)obj;
	Console.WriteLine($"ip:{((TCPConnection)conn).socket.RemoteEndPoint.ToString()},pos({tdata.pos_x},{tdata.pos_y},{tdata.pos_z})");
}