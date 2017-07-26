using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Tests
{
    internal static class IOExtensions
    {

        public static PackStreamReader CreateChunkedPackStreamReaderFromBytes(byte[] bytes, ILogger logger = null)
        {
            MemoryStream mBytesStream = new MemoryStream(bytes);
            ChunkReader chunkReader = new ChunkReader(mBytesStream, logger);

            MemoryStream mStream = new MemoryStream();
            chunkReader.ReadNextChunk(mStream);

            mStream.Position = 0;

            return new PackStreamReader(mStream);
        }

    }
}
