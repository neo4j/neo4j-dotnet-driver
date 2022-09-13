using V4_4 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_4;

namespace Neo4j.Driver.Internal.Protocol
{
	class BoltProtocolV4_4MessageFormat : BoltProtocolV4_3MessageFormat
	{
		#region Message Constants

		#endregion

		internal BoltProtocolV4_4MessageFormat(bool useUtcEncoder):
            base(useUtcEncoder)
		{
			RemoveHandler<V4_3.RouteMessageSerializer>();
			AddHandler<V4_4.RouteMessageSerializer>();
		}
	}
}
