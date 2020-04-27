using System;
using System.Collections.Generic;
using System.Text;
using V3 = Neo4j.Driver.Internal.MessageHandling.V3;
using V4 = Neo4j.Driver.Internal.MessageHandling.V4;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_1 : BoltProtocolV4
    {
        public static readonly BoltProtocolV4_1 BoltV4_1 = new BoltProtocolV4_1();

        public override BoltProtocolVersion Version()
        {
            return new BoltProtocolVersion(4, 1);
        }

    }
}
