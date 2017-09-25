using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class FailureMessageHandler: IPackStreamStructHandler
    {
        public byte Signature => PackStream.MsgFailure;

        public object Read(PackStreamReader reader, long size)
        {
            var values = reader.ReadMap();
            var code = values["code"]?.ToString();
            var message = values["message"]?.ToString();

            return new FailureMessage(code, message);
        }
    }
}
