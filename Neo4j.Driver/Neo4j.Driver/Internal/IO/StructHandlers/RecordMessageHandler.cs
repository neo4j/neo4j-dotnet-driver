using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class RecordMessageHandler: IPackStreamStructHandler
    {
        public byte Signature => PackStream.MsgRecord;

        public object Read(PackStreamReader reader, long size)
        {
            var fieldCount = (int)reader.ReadListHeader();
            var fields = new object[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                fields[i] = reader.Read();
            }

            return new RecordMessage(fields);
        }

    }
}
