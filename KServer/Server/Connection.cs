using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KServer.Server
{
    public abstract class Connection:IDisposable
    {
        /// <summary>
        /// 自定义标识
        /// </summary>
        public string Tag { get; set; } = "";
        /// <summary>
        /// 未响应时长
        /// </summary>
		public int TimeSpend { get; set; } = 0;
		public abstract void Send(byte[] data);
        public abstract Task<byte[]> ReceiveAsync(int length);
        public abstract void Close();

		public void Dispose()
		{
            Close();
		}
    }
}
