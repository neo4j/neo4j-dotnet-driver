using System;
using System.Collections.Generic;
using System.Text;
using V4_3 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_3;
using V4_4 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_4;

namespace Neo4j.Driver.Internal.Protocol
{
	class BoltProtocolV4_4MessageFormat : BoltProtocolV4_3MessageFormat
	{
		internal BoltProtocolV4_4MessageFormat()
		{
			RemoveHandler<V4_3.HelloMessageSerializer>();
			AddHandler<V4_4.HelloMessageSerializer>();
		}
	}
}
