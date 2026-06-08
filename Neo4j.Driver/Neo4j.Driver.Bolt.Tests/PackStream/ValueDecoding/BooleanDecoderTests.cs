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
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class BooleanDecoderTests : DecoderTestsBase<BooleanDecoder>
{
    [Test]
    public void HandlesTrueAndFalseMarkerBytes()
    {
        Subject.HandledMarkerBytes.Should().BeEquivalentTo([PackStreamMarker.True, PackStreamMarker.False]);
    }

    [Test]
    public void DecodesTrue()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.True]);

        var result = Subject.Decode(buffer);

        result.Value.BooleanValue.Should().BeTrue();
        result.BytesConsumed.Should().Be(1);
    }

    [Test]
    public void DecodesFalse()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.False]);

        var result = Subject.Decode(buffer);

        result.Value.BooleanValue.Should().BeFalse();
        result.BytesConsumed.Should().Be(1);
    }

    [Test]
    public void ThrowsOnEmptyBuffer()
    {
        var buffer = ReadOnlySequence<byte>.Empty;

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnUnknownMarker()
    {
        var buffer = new ReadOnlySequence<byte>([0x00]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<InvalidOperationException>();
    }
}
