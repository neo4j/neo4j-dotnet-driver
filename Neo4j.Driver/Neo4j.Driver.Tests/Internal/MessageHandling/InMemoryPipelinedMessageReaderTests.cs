// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling;

/// <summary>
/// Similarly to <see cref="NetworkedPipelinedMessageReaderTests"/>, but this uses in-memory stream instead of
/// network stream.
/// </summary>
public class InMemoryPipelinedMessageReaderTests
{
    private static Mock<IResponsePipeline> MockPipeline()
    {
        var done = (object)false;
        var pipeline = new Mock<IResponsePipeline>();
        pipeline.Setup(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>())).Callback(() => done = true);
        pipeline.SetupGet(x => x.HasNoPendingMessages).Returns(() => (bool)done);
        return pipeline;
    }

    [Fact]
    public async Task ShouldReadMessage()
    {
        using var memoryStream = new MemoryStream(new byte[64]);

        memoryStream.Write(
            new byte[]
            {
                0x00, 0x03,
                PackStream.TinyStruct, MessageFormat.MsgSuccess, PackStream.TinyMap,
                0x00, 0x00
            }.AsSpan());

        memoryStream.Position = 0L;
        var pipereader = new PipelinedMessageReader(memoryStream, TestDriverContext.MockContext, -1);
        var pipeline = MockPipeline();

        await pipereader.ReadAsync(
            pipeline.Object,
            new MessageFormat(
                BoltProtocolVersion.V5_0,
                TestDriverContext.MockContext));

        pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task ShouldReadMultiChunkMessage()
    {
        using var memoryStream = new MemoryStream(new byte[64]);

        memoryStream.Write(
            new byte[]
            {
                0x00, 0x01,
                PackStream.TinyStruct,
                0x00, 0x02,
                MessageFormat.MsgSuccess, PackStream.TinyMap,
                0x00, 0x00
            }.AsSpan());

        memoryStream.Position = 0L;
        var pipereader = new PipelinedMessageReader(memoryStream, TestDriverContext.MockContext, -1);
        var pipeline = MockPipeline();
        await pipereader.ReadAsync(
            pipeline.Object,
            new MessageFormat(
                BoltProtocolVersion.V5_0,
                TestDriverContext.MockContext));

        pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task ShouldIgnoreNoOpChunk()
    {
        var memoryStream = new MemoryStream(new byte[64]);

        memoryStream.Write(
            new byte[]
            {
                0x00, 0x00,
                0x00, 0x02,
                PackStream.TinyStruct, MessageFormat.MsgSuccess,
                0x00, 0x01,
                PackStream.TinyMap,
                0x00, 0x00
            }.AsSpan());

        memoryStream.Position = 0L;
        var pipereader = new PipelinedMessageReader(memoryStream, TestDriverContext.MockContext, -1);
        var pipeline = MockPipeline();
        await pipereader.ReadAsync(
            pipeline.Object,
            new MessageFormat(
                BoltProtocolVersion.V5_0,
                TestDriverContext.MockContext));

        pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task ShouldIgnoreAllNoOpChunks()
    {
        var memoryStream = new MemoryStream(new byte[64]);

        memoryStream.Write(
            new byte[]
            {
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x02,
                PackStream.TinyStruct, MessageFormat.MsgSuccess,
                0x00, 0x01,
                PackStream.TinyMap,
                0x00, 0x00
            }.AsSpan());

        memoryStream.Position = 0L;
        var pipereader = new PipelinedMessageReader(memoryStream, TestDriverContext.MockContext, -1);
        var pipeline = MockPipeline();
        await pipereader.ReadAsync(
            pipeline.Object,
            new MessageFormat(
                BoltProtocolVersion.V5_0,
                TestDriverContext.MockContext));

        pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task ShouldStopReadAllMessagesAfterAFailure()
    {
        var message =
            new byte[]
            {
                // failure
                0x00, 24,
                // 0 bytes
                PackStream.TinyStruct, MessageFormat.MsgFailure, PackStream.TinyMap + 2, PackStream.String8, 0x04,
                // 5 bytes
                0x00, 0x00, 0x00, 0x00,
                // 9 bytes
                PackStream.String8, 1, Encoding.UTF8.GetBytes("a")[0], PackStream.String8, 7,
                // 14 bytes
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // 21 bytes
                PackStream.String8, 1, Encoding.UTF8.GetBytes("b")[0],
                // 24  bytes
                0x00, 0x00,
                // Ignored.
                0x00, 0x02,
                PackStream.TinyStruct, MessageFormat.MsgIgnored,
                0x00, 0x00
            };

        Encoding.UTF8.GetBytes("code").CopyTo(message.AsSpan().Slice(2 + 5));
        Encoding.UTF8.GetBytes("message").CopyTo(message.AsSpan().Slice(2 + 14));

        var memoryStream = new MemoryStream(message);
        var pipereader = new PipelinedMessageReader(memoryStream, TestDriverContext.MockContext, -1);
        var pipeline = MockPipeline();
        await pipereader.ReadAsync(
            pipeline.Object,
            new MessageFormat(
                BoltProtocolVersion.V5_0,
                TestDriverContext.MockContext));

        pipeline.Verify(x => x.OnFailure(It.IsAny<FailureMessage>()), Times.Once);
        pipeline.Verify(x => x.OnIgnored(), Times.Never);
    }

    [Fact]
    public async Task ShouldReadLargeMessages()
    {
        var data = GenerateLargeMessage();
        var memoryStream = new MemoryStream(data);
        var pipereader = new PipelinedMessageReader(memoryStream, TestDriverContext.MockContext, -1);

        var pipeline = MockPipeline();
        await pipereader.ReadAsync(
            pipeline.Object,
            new MessageFormat(
                BoltProtocolVersion.V5_0,
                TestDriverContext.MockContext));

        pipeline.Verify(
            x => x.OnSuccess(It.Is<Dictionary<string, object>>(y => y["a"].As<byte[]>().Length == 500_000)),
            Times.Once);
    }

    private static byte[] GenerateLargeMessage()
    {
        var header = new byte[]
        {
            0x00, 0x0B, // 11 bytes
            PackStream.TinyStruct, MessageFormat.MsgSuccess,
            PackStream.TinyMap + 1,
            PackStream.String8, 0x01, Encoding.UTF8.GetBytes("a")[0],
            PackStream.Bytes32, 0x00, 0x00, 0x00, 0x00 // including the zeros for posterity, these are overwritten
        };

        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(9, 4), 500_000);

        // header, chunks headers, chunks, final header
        var messageLength = 13 + 20 + 500_000 + 2;
        var data = new byte[messageLength];
        header.AsSpan().CopyTo(data.AsSpan());
        for (var i = 0; i < 10; i++)
        {
            var idx = 13 + 2 * i + i * 50_000;
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan().Slice(idx), 50_000);
        }

        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(13 + 20 + 500_000, 2), 0);
        return data;
    }
}
