
using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V4_3
{
	class RouteMessageSerializer : WriteOnlySerializer
    {
        public override IEnumerable<Type> WritableTypes => new[] { typeof(RouteMessage) };

        public override void Serialize(IPackStreamWriter writer, object value)
        {
            var msg = value.CastOrThrow<RouteMessage>();
            
            writer.WriteStructHeader(3, BoltProtocolV4_3MessageFormat.MsgRoute);
            writer.Write(msg.Routing);
			writer.Write(msg.Bookmarks.Values);
			writer.Write(string.IsNullOrEmpty(msg.DatabaseParam) ? null : msg.DatabaseParam);			
		}
    }
}
