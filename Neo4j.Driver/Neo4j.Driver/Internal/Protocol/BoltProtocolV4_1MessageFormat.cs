using V3 = Neo4j.Driver.Internal.IO.MessageSerializers.V3;
using V4_1 =  Neo4j.Driver.Internal.IO.MessageSerializers.V4_1;

namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_1MessageFormat : BoltProtocolV4_0MessageFormat
    {
        internal BoltProtocolV4_1MessageFormat()
        {
            RemoveHandler<V3.HelloMessageSerializer>();
            AddHandler<V4_1.HelloMessageSerializer>();            
        }
    }
}
