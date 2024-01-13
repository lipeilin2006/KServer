using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KServer
{
	[ProtoContract]
	public struct Data
	{
		[ProtoMember(1)]
		public string type;
		[ProtoMember(2)]
		public byte[] data;
	}
}
