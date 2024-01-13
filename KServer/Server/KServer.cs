using KServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace KServer.Server
{
	public abstract class KServer
	{
		public abstract string Host { get; set; }
		public abstract int Port { get; set; }
		public abstract float TimerInterval { get; set; }

		/// <summary>
		/// 连接池
		/// </summary>
		public abstract List<Connection> Connections { get; set; }
		/// <summary>
		/// 反序列化函数表，收到byte[]数据时会根据dataType进行调用
		/// </summary>
		public abstract Dictionary<string, Func<byte[], object>> Deserializers { get; set; }
		/// <summary>
		/// Data处理函数表，收到反序列化后的object对象后会根据dataType进行调用
		/// </summary>
		public abstract Dictionary<string, Action<object, Connection>> DataActions { get; set; }
		/// <summary>
		/// 计时器，用于计算超时
		/// </summary>
		Timer timer;
		/// <summary>
		/// 初始化
		/// </summary>
		public abstract void Init();
		public abstract void Send(Connection conn, byte[] data);
		/// <summary>
		/// 启动！
		/// </summary>
		public void Start()
		{
			//获取整个程序中所有带有自定义标签的函数并加入字典中
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in asm.GetTypes())
				{
					Load(type);
				}
			}
			Init();
			//计时器，用于计算超时
			timer = new(TimerInterval);
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}
		private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
		{
			foreach (Connection connection in Connections)
			{
				connection.TimeSpend += (int)timer.Interval;
			}
		}
		/// <summary>
		/// 加载各种数据处理函数
		/// </summary>
		/// <param name="type"></param>
		private void Load(Type type)
		{
			foreach (MethodInfo method in type.GetRuntimeMethods())
			{
				//获取自定义Attribute对象
				DataDeserializer? dd = method.GetCustomAttribute<DataDeserializer>();
				DataAction? da = method.GetCustomAttribute<DataAction>();
				//添加data反序列化函数
				if (dd!=null)
				{
					if (!Deserializers.ContainsKey(dd.dataType))
					{
						Deserializers.Add(
							dd.dataType,
							object (byte[] data) =>
							{
								return method.Invoke(null, [data]);
							});
						Console.WriteLine($"Loaded deserializer for {dd.dataType}.");
					}
					else
					{
						Console.WriteLine($"{dd.dataType} has already been loaded.");
					}
				}
				//添加取得data后执行的函数
				else if (da!=null)
				{
					if (!Deserializers.ContainsKey(da.dataType))
					{
						DataActions.Add(da.dataType,
						(object obj, Connection conn) =>
						{
							method.Invoke(null, [obj, conn]);
						});
						Console.WriteLine($"Loaded data action for {da.dataType}.");
					}
					else
					{
						Console.WriteLine($"{da.dataType} has already been loaded.");
					}
				}
			}
		}
	}
}