using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Tests
{
    internal static class IOExtensions
    {

        public static PackStreamReader CreateChunkedPackStreamReaderFromBytes(byte[] bytes)
        {
            MemoryStream mBytesStream = new MemoryStream(bytes);
            ChunkReader chunkReader = new ChunkReader(mBytesStream);

            MemoryStream mStream = new MemoryStream();
            chunkReader.ReadNextChunk(mStream);

            mStream.Position = 0;

            return new PackStreamReader(mStream);
        }

    }
}
