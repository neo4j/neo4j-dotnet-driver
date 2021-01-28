// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.IO.MessageSerializers.V3;
using Neo4j.Driver.Internal.IO.ValueSerializers;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Internal.IO
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public class BoltWriterTest
    {
        [Fact]
        public void ShouldThrowWhenWriterIsConstructedUsingNullStream()
        {
            var ex = Record.Exception(() => new MessageWriter(null, CreatePackStreamFactory()));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowWhenWriterIsConstructedUsingNullPackStreamFactory()
        {
            var ex = Record.Exception(() => new MessageWriter(new MemoryStream(), null));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowWhenWriterIsConstructedUsingUnwritableStream()
        {
            var stream = new Mock<Stream>();
            stream.Setup(l => l.CanRead).Returns(false);

            var ex = Record.Exception(() => new MessageWriter(stream.Object, CreatePackStreamFactory()));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldNotWriteToStreamUntilFlushed()
        {
            var stream = new MemoryStream();
            var writer = new MessageWriter(stream, CreatePackStreamFactory());

            writer.Write(new RunWithMetadataMessage(
                new Query("RETURN $x", new Dictionary<string, object> {{"x", 1L}}), AccessMode.Read));

            Assert.Empty(stream.ToArray());
        }

        [Fact]
        public async void ShouldWriteToStreamWhenFlushedAsync()
        {
            var stream = new MemoryStream();
            var writer = new MessageWriter(stream, CreatePackStreamFactory());

            writer.Write(new RunWithMetadataMessage(
                new Query("RETURN $x", new Dictionary<string, object> {{"x", 1L}}), AccessMode.Read));

            await writer.FlushAsync();

            Assert.NotEmpty(stream.ToArray());
        }

        [Fact]
        public void ShouldPropagateErrorOnUnsupportedRequestMessage()
        {
            var stream = new MemoryStream();
            var writer = new MessageWriter(stream, CreatePackStreamFactory());

            var ex = Record.Exception(() => writer.Write(new UnsupportedRequestMessage()));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public async Task ShouldAppendEoMMarkerOnWriteAsync()
        {
            var stream = new MemoryStream();
            var writer = new MessageWriter(stream, CreatePackStreamFactory());

            writer.Write(
                new RunWithMetadataMessage(new Query("RETURN $x", new Dictionary<string, object> {{"x", 1L}}),
                    AccessMode.Read));

            await writer.FlushAsync();

            stream.ToArray().Should().EndWith(new byte[] {0x00, 0x00});
        }

        [Fact]
        public void ShouldThrowWhenReaderIsConstructedUsingNullStream()
        {
            var ex = Record.Exception(() => new MessageReader(null, CreatePackStreamFactory()));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowWhenReaderIsConstructedUsingNullPackStreamFactory()
        {
            var ex = Record.Exception(() => new MessageReader(new MemoryStream(), null));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async void ShouldThrowWhenReadMessageIsNotAResponseMessageAsync()
        {
            var pipeline = new Mock<IResponsePipeline>();
            var reader = new MessageReader(new MemoryStream(CreateNodeMessage()), CreatePackStreamFactory());

            var ex = await Record.ExceptionAsync(() => reader.ReadAsync(pipeline.Object));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>().Subject.Message.Should()
                .StartWith("Unknown response message type");
        }

        [Fact]
        public async Task ShouldReadMessageAsync()
        {
            var pipeline = new Mock<IResponsePipeline>();
            var reader = new MessageReader(new MemoryStream(CreateSuccessMessage()), CreatePackStreamFactory());

            await reader.ReadAsync(pipeline.Object);

            pipeline.Verify(
                x => x.OnSuccess(
                    It.Is<IDictionary<string, object>>(m => m.ContainsKey("x") && m["x"].Equals(1L))), Times.Once);
        }

        [Fact]
        public async Task ShouldReadConsecutiveMessagesAsync()
        {
            var pipeline = new Mock<IResponsePipeline>();

            var stream = new MemoryStream();
            for (var i = 0; i < 5; i++)
            {
                stream.Write(CreateSuccessMessage());
            }

            var reader = new MessageReader(new MemoryStream(stream.ToArray()), CreatePackStreamFactory());
            await reader.ReadAsync(pipeline.Object);

            pipeline.Verify(
                x => x.OnSuccess(
                    It.Is<IDictionary<string, object>>(m => m.ContainsKey("x") && m["x"].Equals(1L))),
                Times.Exactly(5));
        }

        private static IMessageFormat CreatePackStreamFactory()
        {
            var factory = new Mock<IMessageFormat>();

            factory.Setup(x => x.CreateReader(It.IsAny<Stream>()))
                .Returns((Stream stream) => new PackStreamReader(stream, CreateReaderHandlers()));
            factory.Setup(x => x.CreateWriter(It.IsAny<Stream>()))
                .Returns((Stream stream) => new PackStreamWriter(stream, CreateWriterHandlers()));

            return factory.Object;
        }

        private static IDictionary<byte, IPackStreamSerializer> CreateReaderHandlers()
        {
            return new Dictionary<byte, IPackStreamSerializer>
            {
                {BoltProtocolV3MessageFormat.MsgSuccess, new SuccessMessageSerializer()},
                {NodeSerializer.Node, new NodeSerializer()}
            };
        }

        private static IDictionary<Type, IPackStreamSerializer> CreateWriterHandlers()
        {
            return new Dictionary<Type, IPackStreamSerializer>
            {
                {typeof(RunWithMetadataMessage), new RunWithMetadataMessageSerializer()}
            };
        }

        private static byte[] CreateSuccessMessage()
        {
            var stream = new MemoryStream();
            var writer = new PackStreamWriter(stream, null);
            writer.WriteStructHeader(1, BoltProtocolV3MessageFormat.MsgSuccess);
            writer.WriteMapHeader(1);
            writer.Write("x");
            writer.Write(1);

            return CreateChunkedMessage(stream.ToArray());
        }

        private static byte[] CreateNodeMessage()
        {
            var stream = new MemoryStream();
            var writer = new PackStreamWriter(stream, null);
            writer.WriteStructHeader(3, NodeSerializer.Node);
            writer.Write(1L);
            writer.Write(new List<string> {"Label"});
            writer.Write(new Dictionary<string, object>());

            return CreateChunkedMessage(stream.ToArray());
        }

        private static byte[] CreateChunkedMessage(byte[] content)
        {
            var stream = new MemoryStream();
            var chunkWriter = new ChunkWriter(stream);

            chunkWriter.OpenChunk();
            chunkWriter.Write(content, 0, content.Length);
            chunkWriter.CloseChunk();

            chunkWriter.OpenChunk();
            chunkWriter.CloseChunk();

            chunkWriter.Send();

            return stream.ToArray();
        }

        private class UnsupportedRequestMessage : IRequestMessage
        {
        }
    }
}