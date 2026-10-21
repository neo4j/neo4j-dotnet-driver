using System.Buffers;
using System.IO.Pipelines;
using FluentAssertions;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using Neo4j.Driver.Bolt.Transport.Implementations;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests;

[TestFixture]
public class ChunkAssemblerTests : UnitTestBase<ChunkAssembler>
{
    private static IByteReader CreateByteReader(ReadOnlySequence<byte> sequence)
        => new PipeReaderByteReader(PipeReader.Create(sequence));

    private static IByteReader CreateByteReader(byte[] bytes)
        => CreateByteReader(new ReadOnlySequence<byte>(bytes));

    [Test]
    public async Task AssemblesCorrectlyFormedMessage()
    {
        // Arrange
        byte[] bytes =
        [
            0, 10, // there are 10 bytes in this message
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9 // the message payload
        ];

        var reader = CreateByteReader(bytes);
        byte[] expectedBytes = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        // Act
        var messages = await Subject.ReadMessagesAsync(reader).ToListAsync().ConfigureAwait(false);

        // Assert
        messages.Should().HaveCount(1);
        messages[0].ToArray().Should().BeEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task CorrectlyAssemblesChunkedMessage()
    {
        // Arrange
        byte[][] chunks =
        [
            [
                0x00, 0x0A, // there are 10 bytes in this message
                0x00, 0x01, 0x02, 0x03, 0x04, // first chunk of the message payload
            ],
            [
                0x05, 0x06, 0x07, 0x08, 0x09, // second chunk of the message payload
            ]
        ];

        byte[] expectedMessage = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        // Act
        var messages = await TestMessageAssembly(chunks).ConfigureAwait(false);

        // Assert
        var messageResults = messages.ToArray();
        messageResults.Should().HaveCount(1);
        messageResults[0].ToArray().Should().BeEquivalentTo(expectedMessage);
    }

    [Test]
    public async Task ThrowsOnIncompleteHeader()
    {
        byte[] bytes = [0x00]; // Only 1 byte of header
        var reader = CreateByteReader(bytes);
        
        var act = async () => await Subject.ReadMessagesAsync(reader).ToListAsync().ConfigureAwait(false);

        await act.Should().ThrowAsync<ProtocolException>().ConfigureAwait(false);
    }
    
    [Test]
    public async Task ThrowsOnIncompleteBody()
    {
        // Header says 10 bytes, only 2 provided
        byte[] bytes = [0x00, 0x0A, 0x01, 0x02];
        var reader = CreateByteReader(bytes);
    
        var act = async () => await Subject.ReadMessagesAsync(reader).ToListAsync().ConfigureAwait(false);

        await act.Should().ThrowAsync<ProtocolException>().ConfigureAwait(false);
    }

    [Test]
    public async Task HandlesZeroLengthMessage()
    {
        byte[] bytes = [0x00, 0x00]; // Message size = 0
        var reader = CreateByteReader(bytes);

        var messages = await Subject.ReadMessagesAsync(reader).ToListAsync().ConfigureAwait(false);

        messages.Should().HaveCount(1);
        messages[0].Length.Should().Be(0);
    }
    
    [Test]
    public async Task HandlesEmptyPipe()
    {
        var reader = CreateByteReader([]);

        var messages = await Subject.ReadMessagesAsync(reader).ToListAsync().ConfigureAwait(false);

        messages.Should().BeEmpty();
    }
    
    [Test]
    public async Task ThrowsOnCancellation()
    {
        var pipe = new Pipe();
        var reader = new PipeReaderByteReader(pipe.Reader);
        using var cts = new CancellationTokenSource();
    
        var readTask = Subject.ReadMessagesAsync(reader, cts.Token).ToListAsync();
    
        // Cancel before any data arrives
        cts.Cancel();
    
        var act = async () => await readTask.ConfigureAwait(false);

        await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
    }
    
    [Test]
    public async Task ThrowsOnIncompleteSecondMessage()
    {
        // First message complete, second message incomplete
        byte[] bytes =
        [
            0x00, 0x02, 0xAA, 0xBB, // Complete message (2 bytes)
            0x00, 0x05, 0x01        // Incomplete message (header says 5, only 1 provided)
        ];
        var reader = CreateByteReader(bytes);
    
        var act = async () => await Subject.ReadMessagesAsync(reader).ToListAsync().ConfigureAwait(false);

        await act.Should().ThrowAsync<ProtocolException>().ConfigureAwait(false);
    }

    [Test]
    public async Task AssemblesMessageWhenHeaderSplitAcrossTwoChunks()
    {
        // First chunk: only the first byte of the 2-byte length; second chunk: rest of header + body.
        // Message body is 2 bytes: 0xAA, 0xBB.
        byte[][] chunks =
        [
            [0x00],                    // first byte of length (big-endian 0x0002)
            [0x02, 0xAA, 0xBB],        // second byte of length + full body
        ];
        var messages = await TestMessageAssembly(chunks).ConfigureAwait(false);
        messages.Should().HaveCount(1);
        messages[0].Should().BeEquivalentTo([0xAA, 0xBB]);
    }

    [Test]
    public async Task AssemblesTwoMessagesWhenBoundaryFallsInSecondMessageBody()
    {
        // Message 1: body [0xA1]. Message 2: body [0xB1, 0xB2].
        // Chunk 1: full message 1 + length header of message 2 + first byte of message 2 body.
        // Chunk 2: second byte of message 2 body.
        byte[][] chunks =
        [
            [0x00, 0x01, 0xA1, 0x00, 0x02, 0xB1],  // msg1 + msg2 header + 1 byte of msg2 body
            [0xB2],
        ];
        var messages = await TestMessageAssembly(chunks).ConfigureAwait(false);
        messages.Should().HaveCount(2);
        messages[0].Should().BeEquivalentTo([0xA1]);
        messages[1].Should().BeEquivalentTo([0xB1, 0xB2]);
    }

    [Test]
    public async Task AssemblesOneMessageWhenReceivedOneBytePerChunk()
    {
        // Message: 2-byte body. Each of the 4 bytes (header + body) arrives in its own chunk.
        byte[][] chunks =
        [
            [0x00],
            [0x02],
            [0x11],
            [0x22],
        ];
        var messages = await TestMessageAssembly(chunks).ConfigureAwait(false);
        messages.Should().HaveCount(1);
        messages[0].Should().BeEquivalentTo([0x11, 0x22]);
    }

    [Test]
    public async Task AssemblesThreeMessagesWithBoundariesInDifferentPlaces()
    {
        // Message 1: body [0x01]. Message 2: body [0x02, 0x03]. Message 3: body [0x04].
        // Chunk 1: msg1 + msg2 length + first byte of msg2 body.
        // Chunk 2: second byte of msg2 body + full msg3.
        byte[][] chunks =
        [
            [0x00, 0x01, 0x01, 0x00, 0x02, 0x02],  // msg1 + msg2 header + 1 byte
            [0x03, 0x00, 0x01, 0x04],              // rest of msg2 + msg3
        ];
        var messages = await TestMessageAssembly(chunks).ConfigureAwait(false);
        messages.Should().HaveCount(3);
        messages[0].Should().BeEquivalentTo([0x01]);
        messages[1].Should().BeEquivalentTo([0x02, 0x03]);
        messages[2].Should().BeEquivalentTo([0x04]);
    }

    [Test]
    public async Task AssemblesZeroLengthMessageThenNormalMessageSplitAcrossChunks()
    {
        // Message 1: zero-length body. Message 2: body [0xFF].
        // Chunk 1: full zero-length message + first byte of second message's length.
        // Chunk 2: second byte of length + body.
        byte[][] chunks =
        [
            [0x00, 0x00, 0x00],       // msg1 (0x00,0x00) + first byte of msg2 length
            [0x01, 0xFF],              // second byte of length + body
        ];
        var messages = await TestMessageAssembly(chunks).ConfigureAwait(false);
        messages.Should().HaveCount(2);
        messages[0].Should().BeEmpty();
        messages[1].Should().BeEquivalentTo([0xFF]);
    }

    private async Task<byte[][]> TestMessageAssembly(IEnumerable<byte[]> chunks)
    {
        var chunkPipe = new TestChunkPipe(chunks);
        var messages = new List<byte[]>();
        
        var result = Task.Run(async () =>
        {
            await foreach (var readMessage in Subject.ReadMessagesAsync(chunkPipe.Reader).ConfigureAwait(false))
            {
                messages.Add(readMessage.ToArray());
            }
            
            return messages.ToArray();
        });
        
        await chunkPipe.PlayMessages().ConfigureAwait(false);
        return await result.ConfigureAwait(false);
    }

    private class TestChunkPipe
    {
        private readonly IEnumerable<byte[]> _chunks;
        private readonly Pipe _pipe;

        public TestChunkPipe(IEnumerable<byte[]> chunks)
        {
            _chunks = chunks;
            _pipe = new Pipe();
        }

        public IByteReader Reader => new PipeReaderByteReader(_pipe.Reader);

        public async Task PlayMessages()
        {
            foreach (var chunk in _chunks)
            {
                await _pipe.Writer.WriteAsync(chunk).ConfigureAwait(false);
            }

            await _pipe.Writer.CompleteAsync().ConfigureAwait(false);
        }
    }
}
