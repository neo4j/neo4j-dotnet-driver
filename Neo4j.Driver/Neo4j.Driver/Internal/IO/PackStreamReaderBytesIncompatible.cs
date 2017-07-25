using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO
{
    internal class PackStreamReaderBytesIncompatible: PackStreamReader
    {

        public PackStreamReaderBytesIncompatible(Stream stream)
            : base(stream)
        {
            
        }

        public override byte[] UnpackBytes()
        {
            throw new ProtocolException($"Unsupported type {PackStream.PackType.Bytes}.");
        }
    }
}
