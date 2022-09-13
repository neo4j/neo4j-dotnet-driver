using Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_3MessageFormat : BoltProtocolV4_2MessageFormat
    {

        #region Message Constants
        
        public const byte MsgRoute = 0x66 ;
        
        #endregion

        internal BoltProtocolV4_3MessageFormat(bool useUtcEncoder)
        {
            if (!useUtcEncoder)
                return;

            RemoveHandler<ZonedDateTimeSerializer>();
            AddHandler<UtcZonedDateTimeSerializer>();
        }
    }
}