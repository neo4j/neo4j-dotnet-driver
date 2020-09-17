using System;
using System.Collections.Generic;
using System.Text;
using V3 = Neo4j.Driver.Internal.IO.MessageSerializers.V3;
using V4_2 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_2;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_2MessageFormat : BoltProtocolV4_1MessageFormat
    {

        #region Message Constants

        #endregion

        internal BoltProtocolV4_2MessageFormat()
        {
            RemoveHandler<V3.HelloMessageSerializer>();
            AddHandler<V4_2.HelloMessageSerializer>();
        }
    }
}