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
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream;

[TestFixture]
internal class PackStreamValueViewToStringTests
{
    [Test]
    public void ToStringInteger()
    {
        PackStreamValueView.Integer(42).ToString().Should().Be("INT 42");
    }

    [Test]
    public void ToStringFloat()
    {
        PackStreamValueView.Float(1.5).ToString().Should().Be("FLOAT 1.5");
    }

    [Test]
    public void ToStringBoolean()
    {
        PackStreamValueView.Boolean(true).ToString().Should().Be("BOOL True");
        PackStreamValueView.Boolean(false).ToString().Should().Be("BOOL False");
    }

    [Test]
    public void ToStringNull()
    {
        PackStreamValueView.Null().ToString().Should().Be("NULL");
    }

    [Test]
    public void ToStringBytes()
    {
        var bytes = new ReadOnlySequence<byte>([0x01, 0x02, 0x03]);
        PackStreamValueView.Bytes(bytes).ToString().Should().Contain("BYTES[3]");
    }

    [Test]
    public void ToStringString()
    {
        var utf8 = new ReadOnlySequence<byte>([0x68, 0x69]); // "hi"
        PackStreamValueView.String(utf8).ToString().Should().Contain("STRING[2]");
    }

    [Test]
    public void ToStringList()
    {
        var view = PackStreamValueView.List(ReadOnlySequence<byte>.Empty, 0, new StubDecoder());
        view.ToString().Should().Contain("LIST[0]");
    }

    [Test]
    public void ToStringMap()
    {
        var view = PackStreamValueView.Map(ReadOnlySequence<byte>.Empty, 0, new StubDecoder());
        view.ToString().Should().Contain("MAP[0]");
    }

    [Test]
    public void ToStringStruct()
    {
        var listView = new PackStreamListView(ReadOnlySequence<byte>.Empty, 1, new StubDecoder());
        var structView = new PackStreamStructView(0x70, listView);
        var view = PackStreamValueView.Struct(structView);
        view.ToString().Should().Contain("STRUCT[0x70,1]");
    }

    private class StubDecoder : IPackStreamDecoder
    {
        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer) =>
            new(PackStreamValueView.Null(), buffer.IsEmpty ? 0 : 1);

        public async IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }
}
