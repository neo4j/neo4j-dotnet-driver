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

using System.Buffers;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Bolt.Messages.Types;
using Neo4j.Driver.Bolt.Messages.Abstractions.Decoding;
using Neo4j.Driver.Bolt.Messages.Implementations;
using Neo4j.Driver.Bolt.Messages.Implementations.Decoding;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.Messages;

[TestFixture]
internal class BoltStreamDecoderTests
{
    private static ILogger Logger => Mock.Of<ILogger>();

    private static IMessageDecoderProvider CreateMessageDecoderProvider() =>
        new MessageDecoderProvider(
        [
            new SuccessMessageDecoder(Logger),
            new RecordMessageDecoder(Logger),
            new FailureMessageDecoder(Logger),
            new IgnoredMessageDecoder(Logger),
        ]);

    private static PackStreamStructView CreateStructView(byte tag, int fieldCount, IPackStreamDecoder decoder)
    {
        var emptyList = new PackStreamListView(ReadOnlySequence<byte>.Empty, fieldCount, decoder);
        return new PackStreamStructView(tag, emptyList);
    }

    private static IPackStreamDecoder CreatePackStreamDecoderThatYields(params PackStreamValueView[] values)
    {
        return new YieldingPackStreamDecoder(values);
    }

    private class YieldingPackStreamDecoder : IPackStreamDecoder
    {
        private readonly PackStreamValueView[] _values;

        public YieldingPackStreamDecoder(PackStreamValueView[] values) => _values = values;

        public async IAsyncEnumerable<PackStreamValueView> Decode(
            IByteReader byteReader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var value in _values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return value;
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer) =>
            new(PackStreamValueView.Null(), buffer.IsEmpty ? 0 : 1);
    }

    [Test]
    public async Task ReadMessagesAsyncYieldsSuccessMessageWhenStreamYieldsStruct()
    {
        var stubDecoder = new StubPackStreamDecoder();
        var structView = CreateStructView(0x70, 1, stubDecoder); // SUCCESS
        var packStreamDecoder = CreatePackStreamDecoderThatYields(PackStreamValueView.Struct(structView));
        var messageProvider = CreateMessageDecoderProvider();
        var boltDecoder = new BoltStreamDecoder(packStreamDecoder, messageProvider, Logger);
        var byteReader = new Mock<IByteReader>().Object;

        var messages = await boltDecoder.ReadMessagesAsync(byteReader).ToListAsync().ConfigureAwait(false);

        messages.Should().HaveCount(1);
        messages[0].Kind.Should().Be(MessageKind.Success);
        messages[0].AsSuccess();
    }

    [Test]
    public async Task ReadMessagesAsyncYieldsMultipleMessagesInOrder()
    {
        var stubDecoder = new StubPackStreamDecoder();
        var success = CreateStructView(0x70, 1, stubDecoder);
        var ignored = CreateStructView(0x7E, 0, stubDecoder);
        var packStreamDecoder = CreatePackStreamDecoderThatYields(
            PackStreamValueView.Struct(success),
            PackStreamValueView.Struct(ignored));
        var boltDecoder = new BoltStreamDecoder(
            packStreamDecoder,
            CreateMessageDecoderProvider(),
            Logger);
        var byteReader = new Mock<IByteReader>().Object;

        var messages = await boltDecoder.ReadMessagesAsync(byteReader).ToListAsync().ConfigureAwait(false);

        messages.Should().HaveCount(2);
        messages[0].Kind.Should().Be(MessageKind.Success);
        messages[1].Kind.Should().Be(MessageKind.Ignored);
    }

    [Test]
    public async Task ReadMessagesAsyncThrowsInvalidOperationExceptionWhenValueIsNotStruct()
    {
        var packStreamDecoder = CreatePackStreamDecoderThatYields(PackStreamValueView.Integer(42));
        var boltDecoder = new BoltStreamDecoder(
            packStreamDecoder,
            CreateMessageDecoderProvider(),
            Logger);
        var byteReader = new Mock<IByteReader>().Object;

        var act = async () => await boltDecoder.ReadMessagesAsync(byteReader).ToListAsync().ConfigureAwait(false);

        await act.Invoking(a => a()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task ReadMessagesAsyncThrowsKeyNotFoundExceptionWhenNoDecoderForStructTag()
    {
        var stubDecoder = new StubPackStreamDecoder();
        var unknownTagStruct = CreateStructView(0x99, 0, stubDecoder);
        var packStreamDecoder = CreatePackStreamDecoderThatYields(PackStreamValueView.Struct(unknownTagStruct));
        var boltDecoder = new BoltStreamDecoder(
            packStreamDecoder,
            CreateMessageDecoderProvider(),
            Logger);
        var byteReader = new Mock<IByteReader>().Object;

        var act = async () => await boltDecoder.ReadMessagesAsync(byteReader).ToListAsync().ConfigureAwait(false);

        await act.Invoking(a => a()).Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public void ConstructorThrowsOnNullPackStreamDecoder()
    {
        var act = () => new BoltStreamDecoder(
            null!,
            CreateMessageDecoderProvider(),
            Logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void ConstructorThrowsOnNullMessageDecoderProvider()
    {
        var packStreamDecoder = CreatePackStreamDecoderThatYields();

        var act = () => new BoltStreamDecoder(packStreamDecoder, null!, Logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void ConstructorThrowsOnNullLogger()
    {
        var packStreamDecoder = CreatePackStreamDecoderThatYields();

        var act = () => new BoltStreamDecoder(packStreamDecoder, CreateMessageDecoderProvider(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private class StubPackStreamDecoder : IPackStreamDecoder
    {
        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer) =>
            new(PackStreamValueView.Null(), buffer.IsEmpty ? 0 : 1);

        public IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
