using System;
using System.Collections.Generic;
using System.Text;
using V3 = Neo4j.Driver.Internal.IO.MessageSerializers.V3;
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

			RemoveHandler<V3.BeginMessageSerializer>();
			AddHandler<V4_4.BeginMessageSerializer>();

			RemoveHandler<V4_3.RouteMessageSerializer>();
			AddHandler<V4_4.RouteMessageSerializer>();
		}
	}
}
