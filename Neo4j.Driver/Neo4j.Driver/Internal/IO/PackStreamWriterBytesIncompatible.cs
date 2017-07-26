using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO
{
    internal class PackStreamWriterBytesIncompatible: PackStreamWriter
    {

        public PackStreamWriterBytesIncompatible(Stream stream)
            : base(stream)
        {
            
        }

        public override void Write(byte[] values)
        {
            throw new ProtocolException($"Cannot understand { nameof(values) } with type { values.GetType().FullName}");
        }
    }
}
