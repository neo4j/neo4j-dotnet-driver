using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class IgnoredMessageHandler: IPackStreamStructHandler
    {
        public byte Signature => PackStream.MsgIgnored;

        public object Read(PackStreamReader reader, long size)
        {
            return new IgnoredMessage();
        }

    }
}
