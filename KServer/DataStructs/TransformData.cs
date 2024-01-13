using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace KServer.DataStructs
{
	[ProtoContract]
	public struct TransformData
	{
		[ProtoMember(1)]
		public float pos_x;
		[ProtoMember(2)]
		public float pos_y;
		[ProtoMember(3)]
		public float pos_z;
		[ProtoMember(4)]
		public float rot_x;
		[ProtoMember(5)]
		public float rot_y;
		[ProtoMember(6)]
		public float rot_z;
		[ProtoMember(7)]
		public float scale_x;
		[ProtoMember(8)]
		public float scale_y;
		[ProtoMember(9)]
		public float scale_z;
	}
}
