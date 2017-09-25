using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class SuccessMessageHandler: IPackStreamStructHandler
    {
        public byte Signature => PackStream.MsgSuccess;

        public object Read(PackStreamReader reader, long size)
        {
            var map = reader.ReadMap();

            return new SuccessMessage(map);
        }
    }
}
