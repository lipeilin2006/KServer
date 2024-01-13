using KServer.Attributes;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KServer.Server
{
	public class UDPServer:KServer
	{
		private string host = "";
		private int port;
		private List<Connection> connections = new();
		private Dictionary<string, Func<byte[], object>> deserializers = new();
		private Dictionary<string, Action<object, Connection>> dataActions = new();
		private Socket? udpsocket;
		private bool isStop = false;
		private ConcurrentQueue<Connection> inlineConn = new ConcurrentQueue<Connection>();

		/// <summary>
		/// 最大SocketAsyncEventArgs数量，服务器开启时创建
		/// </summary>
		public int MaxAsyncEvent { get; set; } = 100;
		/// <summary>
		/// 超时断连时间
		/// </summary>
		public int Timeout { get; set; } = 10000;
		/// <summary>
		/// 收到连接后执行，通常用于身份验证
		/// </summary>
		public Action OnConnected { get; set; } = () => { };
		/// <summary>
		/// 主机名
		/// </summary>
		public override string Host { get { return host; } set { host = value ?? ""; } }
		/// <summary>
		/// 端口
		/// </summary>
		public override int Port { get { return port; } set { port = value; } }
		/// <summary>
		/// 连接池
		/// </summary>
		public override List<Connection> Connections { get { return connections; } set { connections = value; } }
		/// <summary>
		/// 反序列化函数表，收到byte[]数据时会根据dataType进行调用
		/// </summary>
		public override Dictionary<string, Func<byte[], object>> Deserializers { get { return deserializers; } set { deserializers = value; } }
		/// <summary>
		/// Data处理函数表，收到反序列化后的object对象后会根据dataType进行调用
		/// </summary>
		public override Dictionary<string, Action<object, Connection>> DataActions { get { return dataActions; } set { dataActions = value; } }
		/// <summary>
		/// Timer的触发频率，用于计算超时
		/// </summary>
		public override float TimerInterval { get; set; } = 100f;

		/// <summary>
		/// Server初始化函数，无需自行调用
		/// </summary>
		public override void Init()
		{
			udpsocket = new(SocketType.Dgram, ProtocolType.Udp);
			udpsocket.Bind(new IPEndPoint(IPAddress.Parse(host ?? "0.0.0.0"), port));
			udpsocket.Listen();
			Console.WriteLine("\nCreating socket async event args.");

			for (int i = 0; i < MaxAsyncEvent; i++)
			{
				SocketAsyncEventArgs e = new();
				StartAccept(e);
			}

			new Thread(async () => { await ListenLoop(); }).Start();
			Console.WriteLine($"UDPServer started at {host}:{port}.");
		}

		/// <summary>
		/// 开始面对单个SocketAsyncEventArgs接收连接
		/// </summary>
		/// <param name="e"></param>
		private void StartAccept(SocketAsyncEventArgs e)
		{
			if (isStop)
			{
				e.Completed += SocketAsyncEventArgs_Completed;
				//请放心，tcpsocket在这里不为空
				if (!udpsocket.AcceptAsync(e))
				{
					Console.WriteLine("Connected");
					ProcessAccept(e);
				}
			}
		}

		/// <summary>
		/// SocketAsyncEventArgs完成连接建立后的回调函数
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SocketAsyncEventArgs_Completed(object? sender, SocketAsyncEventArgs e)
		{
			Console.WriteLine("Connected");
			OnConnected();
			ProcessAccept(e);
		}

		/// <summary>
		/// 处理收到的连接
		/// </summary>
		/// <param name="e"></param>
		private void ProcessAccept(SocketAsyncEventArgs e)
		{
			inlineConn.Enqueue(new TCPConnection(e.AcceptSocket));
			//SocketAsyncEventArgs循环利用
			e.AcceptSocket = null;
			StartAccept(e);
		}

		/// <summary>
		/// 循环监听连接池中的连接
		/// </summary>
		/// <returns></returns>
		private async Task ListenLoop()
		{
			while (!isStop)
			{
				while (inlineConn.Count > 0)
				{
					Connection conn;
					if (inlineConn.TryDequeue(out conn))
					{
						connections.Add(conn);
					}
				}
				for (int i = 0; i < connections.Count; i++)
				{
					Connection conn_this = connections[i];
					try
					{
						//接受头部
						byte[] head = await conn_this.ReceiveAsync(sizeof(Int32));
						//byte[]转int，应接收的data长度
						int length = BitConverter.ToInt32(head, 0);
						//反序列化出Data对象
						byte[] buffer = await conn_this.ReceiveAsync(length);
						Data data = Serializer.Deserialize<Data>(new MemoryStream(buffer));
						Console.WriteLine("ReceiveData.");
						//调用注册的数据包处理函数
						if (deserializers.ContainsKey(data.type) && dataActions.ContainsKey(data.type))
						{
							dataActions[data.type](deserializers[data.type](data.data), conn_this);
						}
						conn_this.TimeSpend = 0;
					}
					catch (Exception ex)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"{ex.GetType().Name}:{ex.Message}");
						Console.ResetColor();
					}
					finally
					{
						if (conn_this.TimeSpend >= Timeout)
						{
							connections[i].Close();
							connections.RemoveAt(i);
							i -= 1;
						}
					}
				}
			}
		}

		public void Dispose()
		{
			isStop = true;
			Thread.Sleep(1000);
			udpsocket?.Close();
		}

		/// <summary>
		/// 向Connection的对端发送数据
		/// </summary>
		/// <param name="conn">Connection连接</param>
		/// <param name="data">要发送的数据</param>
		public override void Send(Connection conn, byte[] data)
		{
			conn.Send(data);
		}
	}
	public class UDPConnection(Socket? s) : Connection
	{
		public Socket? socket = s;

		/// <summary>
		/// 关闭连接
		/// </summary>
		public override void Close()
		{
			socket?.Close();
		}

		/// <summary>
		/// 异步接收数据
		/// </summary>
		/// <param name="length">要接收的数据长度</param>
		/// <returns></returns>
		/// <exception cref="Exception">接收出错</exception>
		public override async Task<byte[]> ReceiveAsync(int length)
		{
			if (socket != null)
			{
				byte[] data = new byte[length];
				int length_fix = await socket.ReceiveAsync(data);
				return data.Take(length_fix).ToArray();
			}
			else
			{
				throw new Exception("Receive Error");
			}
		}

		/// <summary>
		/// 异步发送数据
		/// </summary>
		/// <param name="data">要发送的数据</param>
		/// <exception cref="Exception">发送出错</exception>
		public override void Send(byte[] data)
		{
			if (socket != null)
			{
				socket.Send(data);
			}
			else
			{
				throw new Exception("Send Error");
			}
		}
	}
}
