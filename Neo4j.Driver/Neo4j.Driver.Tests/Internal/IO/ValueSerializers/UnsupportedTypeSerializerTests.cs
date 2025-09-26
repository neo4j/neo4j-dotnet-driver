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
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers;

public class UnsupportedTypeSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest { get; } = new UnsupportedTypeSerializer();

    [Fact]
    public void ShouldDeserialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(4, (byte)'?');
        writer.WriteString("the_type");
        writer.WriteByte(42);
        writer.WriteByte(69);
        writer.WriteMapHeader(1);
        writer.WriteString("message");
        writer.WriteString("This is the message");

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        value.Should().BeOfType<UnsupportedType>();
        var unsupported = (UnsupportedType)value;
        unsupported.Name.Should().Be("the_type");
        unsupported.MinimumProtocolVersion.Should().Be("42.69");
        unsupported.Message.Should().Be("This is the message");
    }

    [Fact]
    public void ShouldThrowOnWrongSignature()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(4, 0x01); // Wrong signature
        writer.WriteString("the_type");
        writer.WriteByte(6);
        writer.WriteByte(0);
        writer.WriteMapHeader(1);
        writer.WriteString("message");
        writer.WriteString("msg");

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        FluentActions.Invoking(() =>
                SerializerUnderTest.Deserialize(BoltProtocolVersion.V6_0, reader, 0x01, 4))
            .Should().Throw<ProtocolException>();
    }

    [Fact]
    public void ShouldThrowOnWrongStructSize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(3, (byte)'?'); // Wrong size
        writer.WriteString("the_type");
        writer.WriteByte(6);
        writer.WriteByte(0);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        FluentActions.Invoking(() =>
                SerializerUnderTest.Deserialize(BoltProtocolVersion.V6_0, reader, (byte)'?', 3))
            .Should().Throw<ClientException>();
    }

    [Fact]
    public void ShouldThrowIfMessageMissing()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(4, (byte)'?');
        writer.WriteString("the_type");
        writer.WriteByte(6);
        writer.WriteByte(0);
        writer.WriteMapHeader(0); // No "message" field

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        FluentActions.Invoking(() =>
                SerializerUnderTest.Deserialize(BoltProtocolVersion.V6_0, reader, (byte)'?', 4))
            .Should().Throw<ProtocolException>();
    }

    [Fact]
    public void ShouldThrowIfMessageNotString()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(4, (byte)'?');
        writer.WriteString("the_type");
        writer.WriteByte(6);
        writer.WriteByte(0);
        writer.WriteMapHeader(1);
        writer.WriteString("message");
        writer.WriteByte(123); // Not a string

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        FluentActions.Invoking(() =>
                SerializerUnderTest.Deserialize(BoltProtocolVersion.V6_0, reader, (byte)'?', 4))
            .Should().Throw<ProtocolException>();
    }

    [Fact]
    public void ShouldThrowOnSerialize()
    {
        FluentActions.Invoking(() =>
                SerializerUnderTest.Serialize(BoltProtocolVersion.V6_0, null, new object()))
            .Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void DeserializeSpanShouldMatchDeserialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(4, (byte)'?');
        writer.WriteString("Vector");
        writer.WriteByte(6);
        writer.WriteByte(0);
        writer.WriteMapHeader(1);
        writer.WriteString("message");
        writer.WriteString("A");

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var result = reader.Read();
        result.Should().BeOfType<UnsupportedType>();
        var unsupported = (UnsupportedType)result;
        unsupported.Name.Should().Be("Vector");
        unsupported.MinimumProtocolVersion.Should().Be("6.0");
        unsupported.Message.Should().Be("A");
    }
}
