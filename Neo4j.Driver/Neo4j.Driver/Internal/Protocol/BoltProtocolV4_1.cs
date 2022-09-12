using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_1 : BoltProtocolV4_0
    {
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_1;
        public override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4_1;
    }
}
