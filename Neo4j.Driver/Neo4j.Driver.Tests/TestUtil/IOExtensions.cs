// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Tests
{
    internal static class IOExtensions
    {

        public static byte[] GenerateBoltMessage(int paramBytesLength)
        {
            using (var data = new MemoryStream())
            {
                var boltWriter = new BoltWriter(data);
                boltWriter.Write(new RunMessage("RETUN {a}", new Dictionary<string, object>
                {
                    {"a", Enumerable.Repeat((byte) 0, paramBytesLength).ToArray()}
                }));
                boltWriter.Flush();

                return data.ToArray();
            }
        }

        public static byte[] GenerateBoltMessages(int paramBytesLength, int limit)
        {
            using (var data = new MemoryStream())
            {
                var boltWriter = new BoltWriter(data);

                while (data.Length < limit)
                {
                    boltWriter.Write(new RunMessage("RETUN {a}", new Dictionary<string, object>
                    {
                        {"a", Enumerable.Repeat((byte) 0, paramBytesLength).ToArray()}
                    }));
                    boltWriter.Flush();
                }

                return data.ToArray();
            }
        }

        public static PackStreamReader CreateChunkedPackStreamReaderFromBytes(byte[] bytes, ILogger logger = null)
        {
            MemoryStream mBytesStream = new MemoryStream(bytes);
            ChunkReader chunkReader = new ChunkReader(mBytesStream, logger);

            MemoryStream mStream = new MemoryStream();
            chunkReader.ReadNextMessages(mStream);

            mStream.Position = 0;

            return new PackStreamReader(mStream, BoltReader.StructHandlers);
        }

        public static Mock<MemoryStream> CreateMockStream(string bytesAsHexString)
        {
            return CreateMockStream(bytesAsHexString.ToByteArray());
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
            mockInput.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).CallBase();


            return mockInput;
        }

    }
}
