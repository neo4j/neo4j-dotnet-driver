using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
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

        public static Mock<MemoryStream> CreateMockStream(params byte[] bytes)
        {
            return CreateMockStream(bytes, new byte[0]);
        }

        public static Mock<MemoryStream> CreateMockStream(byte b, params byte[] bytes)
        {
            return CreateMockStream(new byte[] {b}, bytes);
        }

        public static Mock<MemoryStream> CreateMockStream(params byte[][] buffers)
        {
            MemoryStream tmpStream = new MemoryStream();
            foreach (var buffer in buffers)
            {
                tmpStream.Write(buffer, 0, buffer.Length);
            }

            var mockInput = new Mock<MemoryStream>(tmpStream.ToArray());

            mockInput.Setup(x => x.Position).CallBase();
            mockInput.Setup(x => x.Length).CallBase();
            mockInput.Setup(x => x.Position).CallBase();
            mockInput.Setup(x => x.CanRead).CallBase();
            mockInput.Setup(x => x.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>())).CallBase();
            mockInput.Setup(x => x.ReadByte()).CallBase();
            mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).CallBase();

            return mockInput;
        }

    }
}
