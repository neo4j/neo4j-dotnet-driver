using System;
using System.Collections.Generic;
using System.Text;
using V4_1 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_1;
using V4_2 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_2;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_2MessageFormat : BoltProtocolV4_1MessageFormat
    {

        #region Message Constants

        #endregion

        internal BoltProtocolV4_2MessageFormat()
        {
            RemoveHandler<V4_1.HelloMessageSerializer>();
            AddHandler<V4_2.HelloMessageSerializer>();
        }
    }
}