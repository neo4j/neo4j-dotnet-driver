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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Bolt.Messages.Types;
using Neo4j.Driver.Bolt.Messages.Abstractions.Decoding;
using Neo4j.Driver.Bolt.Messages.Implementations.Decoding;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.PackStream.Implementations;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.Messages;

[TestFixture]
internal class MessageDecoderTests
{
    private static ILogger Logger => Mock.Of<ILogger>();

    [Test]
    public void SuccessMessageDecoderHandledTagMatchesMessageKindSuccess()
    {
        var decoder = new SuccessMessageDecoder(Logger);
        decoder.HandledTag.Should().Be((byte)MessageKind.Success).And.Be(0x70);
    }

    [Test]
    public void RecordMessageDecoderHandledTagMatchesMessageKindRecord()
    {
        var decoder = new RecordMessageDecoder(Logger);
        decoder.HandledTag.Should().Be((byte)MessageKind.Record).And.Be(0x71);
    }

    [Test]
    public void FailureMessageDecoderHandledTagMatchesMessageKindFailure()
    {
        var decoder = new FailureMessageDecoder(Logger);
        decoder.HandledTag.Should().Be((byte)MessageKind.Failure).And.Be(0x7F);
    }

    [Test]
    public void IgnoredMessageDecoderHandledTagMatchesMessageKindIgnored()
    {
        var decoder = new IgnoredMessageDecoder(Logger);
        decoder.HandledTag.Should().Be((byte)MessageKind.Ignored).And.Be(0x7E);
    }

    [Test]
    public void MessageDecoderProviderDecodesSuccessStructIntoBoltMessage()
    {
        var structView = CreateStructView(0x70, 1, new StubPackStreamDecoder()); // SUCCESS, 1 field (metadata)
        var provider = CreateProvider();

        var message = DecodeMessage(provider, structView);

        message.Kind.Should().Be(MessageKind.Success);
        message.AsSuccess(); // view is constructible
    }

    [Test]
    public void MessageDecoderProviderDecodesRecordStructIntoBoltMessage()
    {
        var structView = CreateStructView(0x71, 1, new StubPackStreamDecoder()); // RECORD, 1 field (list)
        var provider = CreateProvider();

        var message = DecodeMessage(provider, structView);

        message.Kind.Should().Be(MessageKind.Record);
        message.AsRecord(); // view is constructible
    }

    [Test]
    public void MessageDecoderProviderDecodesFailureStructIntoBoltMessage()
    {
        var structView = CreateStructView(0x7F, 1, new StubPackStreamDecoder()); // FAILURE, 1 field (map)
        var provider = CreateProvider();

        var message = DecodeMessage(provider, structView);

        message.Kind.Should().Be(MessageKind.Failure);
        message.AsFailure(); // view is constructible
    }

    [Test]
    public void MessageDecoderProviderDecodesIgnoredStructIntoBoltMessage()
    {
        var structView = CreateStructView(0x7E, 0, new StubPackStreamDecoder()); // IGNORED, 0 fields
        var provider = CreateProvider();

        var message = DecodeMessage(provider, structView);

        message.Kind.Should().Be(MessageKind.Ignored);
        message.AsIgnored();
    }

    [Test]
    public void BoltMessageAsSuccessThrowsWhenKindIsNotSuccess()
    {
        var structView = CreateStructView(0x7E, 0, new StubPackStreamDecoder()); // Ignored
        var message = DecodeMessage(CreateProvider(), structView);
        message.Kind.Should().Be(MessageKind.Ignored);

        var act = () => message.AsSuccess();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void BoltMessageAsRecordThrowsWhenKindIsNotRecord()
    {
        var structView = CreateStructView(0x70, 1, new StubPackStreamDecoder());
        var message = DecodeMessage(CreateProvider(), structView);

        var act = () => message.AsRecord();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void BoltMessageAsFailureThrowsWhenKindIsNotFailure()
    {
        var structView = CreateStructView(0x70, 1, new StubPackStreamDecoder());
        var message = DecodeMessage(CreateProvider(), structView);

        var act = () => message.AsFailure();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void BoltMessageAsIgnoredThrowsWhenKindIsNotIgnored()
    {
        var structView = CreateStructView(0x70, 1, new StubPackStreamDecoder());
        var message = DecodeMessage(CreateProvider(), structView);

        var act = () => message.AsIgnored();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void MessageDecoderProviderTryGetDecoderReturnsFalseForUnknownTag()
    {
        var provider = CreateProvider();

        var found = provider.TryGetDecoder(0x99, out var decoder);

        found.Should().BeFalse();
        decoder.Should().BeNull();
    }

    [Test]
    public void MessageDecoderProviderTryGetDecoderReturnsTrueAndDecoderIsNotNullForKnownTags()
    {
        var provider = CreateProvider();
        var stubDecoder = new StubPackStreamDecoder();

        (byte tag, MessageKind expectedKind, int fieldCount)[] known =
        [
            (0x70, MessageKind.Success, 1),
            (0x71, MessageKind.Record, 1),
            (0x7F, MessageKind.Failure, 1),
            (0x7E, MessageKind.Ignored, 0),
        ];

        foreach (var (tag, expectedKind, fieldCount) in known)
        {
            var found = provider.TryGetDecoder(tag, out var decoder);
            found.Should().BeTrue($"tag 0x{tag:X2} is registered");
            decoder.Should().NotBeNull();
            var structView = CreateStructView(tag, fieldCount, stubDecoder);
            var message = decoder!.Decode(structView);
            message.Kind.Should().Be(expectedKind);
        }
    }

    [Test]
    public void SuccessMessageViewExposesMetadataFromStructField()
    {
        // PackStream: SUCCESS struct (tag 0x70) with one field = map {"server" -> "Neo4j/5.0"}
        // Tiny struct 1 field: 0xB1, tag 0x70, then map 1 entry
        // Tiny map 1: 0xA1, key "server" (6 chars): 0x86 + UTF-8, value "Neo4j/5.0" (9 chars): 0x89 + UTF-8
        byte[] successStructBytes =
        [
            0xB1, 0x70, // struct 1 field, tag SUCCESS
            0xA1, // map 1 entry
            0x86, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, // "server" (6 bytes)
            0x89, 0x4E, 0x65, 0x6F, 0x34, 0x6A, 0x2F, 0x35, 0x2E, 0x30, // "Neo4j/5.0" (9 bytes)
        ];

        var packStreamDecoder = CreatePackStreamDecoder();
        var result = packStreamDecoder.Decode(new ReadOnlySequence<byte>(successStructBytes));
        result.Value.Type.Should().Be(PackStreamType.Struct);

        var structView = result.Value.StructValue;
        var message = DecodeMessage(CreateProvider(), structView);
        message.Kind.Should().Be(MessageKind.Success);

        var success = message.AsSuccess();
        success.Metadata.Count.Should().Be(1);
        var metadata = success.Metadata.ToEnumerable().ToList();
        metadata.Should().HaveCount(1);
        metadata[0].Key.Type.Should().Be(PackStreamType.String);
        metadata[0].Key.StringValue.ToString().Should().Be("server");
        metadata[0].Value.Type.Should().Be(PackStreamType.String);
        metadata[0].Value.StringValue.ToString().Should().Be("Neo4j/5.0");

        var dict = success.Metadata.ToEnumerable()
            .ToDictionary(kv => kv.Key.StringValue.ToString(), kv => kv.Value.StringValue.ToString());

        dict.Should().Contain("server", "Neo4j/5.0");
    }

    [Test]
    public void RecordMessageViewExposesFieldsListFromStructField()
    {
        // PackStream: RECORD struct (tag 0x71) with one field = list [42, "hello"]
        // Tiny struct 1 field: 0xB1, tag 0x71, then list 2 items: 0x92, 0x2A, 0x85 0x68 0x65 0x6C 0x6F
        byte[] recordStructBytes =
        [
            0xB1, 0x71, // struct 1 field, tag RECORD
            0x92, // list 2 items
            0x2A, // 42
            0x85, 0x68, 0x65, 0x6C, 0x6C, 0x6F, // "hello"
        ];

        var packStreamDecoder = CreatePackStreamDecoder();
        var result = packStreamDecoder.Decode(new ReadOnlySequence<byte>(recordStructBytes));
        result.Value.Type.Should().Be(PackStreamType.Struct);

        var structView = result.Value.StructValue;
        var message = DecodeMessage(CreateProvider(), structView);
        message.Kind.Should().Be(MessageKind.Record);

        var record = message.AsRecord();
        record.Fields.Count.Should().Be(2);
        var fields = record.Fields.ToEnumerable().ToList();
        fields.Should().HaveCount(2);
        fields[0].Type.Should().Be(PackStreamType.Integer);
        fields[0].IntValue.Should().Be(42);
        fields[1].Type.Should().Be(PackStreamType.String);
        fields[1].StringValue.ToString().Should().Be("hello");
    }

    [Test]
    public void FailureMessageViewExposesMetadataFromStructField()
    {
        // PackStream: FAILURE struct (tag 0x7F) with one field = map {"code" -> "X", "message" -> "Y"}
        // Tiny struct 1 field: 0xB1, tag 0x7F, then map 2 entries
        byte[] failureStructBytes =
        [
            0xB1, 0x7F, // struct 1 field, tag FAILURE
            0xA2, // map 2 entries
            0x84, 0x63, 0x6F, 0x64, 0x65, 0x81, 0x58, // "code" -> "X"
            0x87, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x81, 0x59, // "message" -> "Y"
        ];

        var packStreamDecoder = CreatePackStreamDecoder();
        var result = packStreamDecoder.Decode(new ReadOnlySequence<byte>(failureStructBytes));
        result.Value.Type.Should().Be(PackStreamType.Struct);

        var structView = result.Value.StructValue;
        var message = DecodeMessage(CreateProvider(), structView);
        message.Kind.Should().Be(MessageKind.Failure);

        var failure = message.AsFailure();
        failure.Metadata.Count.Should().Be(2);
        var metadata = failure.Metadata.ToEnumerable()
            .ToDictionary(x => x.Key.StringValue.ToString(), x => x.Value.StringValue.ToString());

        metadata.Should().Contain("code", "X");
        metadata.Should().Contain("message", "Y");
    }

    private static IPackStreamDecoder CreatePackStreamDecoder()
    {
        var decoders = new IValueDecoder[]
        {
            new NullDecoder(Logger),
            new BooleanDecoder(Logger),
            new TinyIntDecoder(Logger),
            new IntegerDecoder(Logger),
            new FloatDecoder(Logger),
            new StringDecoder(Logger),
            new BytesDecoder(Logger),
            new ListDecoder(Logger),
            new MapDecoder(Logger),
            new StructDecoder(Logger),
        };

        var provider = new ValueDecoderProvider(decoders, Logger);
        return new PackStreamDecoder(Mock.Of<IChunkAssembler>(), provider, Logger);
    }

    private static IMessageDecoderProvider CreateProvider()
    {
        return new MessageDecoderProvider(
        [
            new SuccessMessageDecoder(Logger),
            new RecordMessageDecoder(Logger),
            new FailureMessageDecoder(Logger),
            new IgnoredMessageDecoder(Logger),
        ]);
    }

    private static BoltResponseMessage DecodeMessage(IMessageDecoderProvider provider, PackStreamStructView structView)
    {
        if (!provider.TryGetDecoder(structView.Tag, out var decoder))
        {
            throw new KeyNotFoundException($"No message decoder registered for tag 0x{structView.Tag:X2}.");
        }

        return decoder.Decode(structView);
    }

    private static PackStreamStructView CreateStructView(byte tag, int fieldCount, IPackStreamDecoder decoder)
    {
        var emptyList = new PackStreamListView(ReadOnlySequence<byte>.Empty, fieldCount, decoder);
        return new PackStreamStructView(tag, emptyList);
    }

    private class StubPackStreamDecoder : IPackStreamDecoder
    {
        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer) =>
            new(PackStreamValueView.Null(), buffer.IsEmpty ? 0 : 1);

        public IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
