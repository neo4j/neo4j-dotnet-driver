using System;
using System.Collections.Generic;
using System.Text;
using V4_2 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_2;
using V4_3 = Neo4j.Driver.Internal.IO.MessageSerializers.V4_3;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_3MessageFormat : BoltProtocolV4_2MessageFormat
    {

        #region Message Constants
        
        public const byte MsgRoute = 0x66 ;
        
        #endregion

        internal BoltProtocolV4_3MessageFormat()
        {
            RemoveHandler<V4_2.HelloMessageSerializer>();
            AddHandler<V4_3.HelloMessageSerializer>();

            //TODO: ROUTE - Add message serializer here...
            //AddHandler<V4_3.RouteMessageSerializer>();
        }
    }
}