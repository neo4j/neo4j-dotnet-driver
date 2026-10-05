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
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using Neo4j.Driver.Bolt.Tests.TestHelpers;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class TinyIntDecoderTests : DecoderTestsBase<TinyIntDecoder>
{
    [Test]
    public void HandledMarkerBytesIncludesPositiveAndNegativeTinyInts()
    {
        var expected = new ByteArrayBuilder()
            .Range(..0x80)
            .Range(0xF0..0x100);
            
        Subject.HandledMarkerBytes.Should().BeEquivalentTo(expected.Bytes);
    }

    [Test]
    [TestCase((byte)0x00, 0, TestName = "DecodesTinyInt_Zero")]
    [TestCase((byte)0x01, 1, TestName = "DecodesTinyInt_One")]
    [TestCase((byte)0x2A, 42, TestName = "DecodesTinyInt_42")]
    [TestCase((byte)0x7F, 127, TestName = "DecodesTinyInt_MaxPositive_127")]
    [TestCase((byte)0xF0, -16, TestName = "DecodesTinyInt_MinNegative_Minus16")]
    [TestCase((byte)0xFF, -1, TestName = "DecodesTinyInt_MinusOne")]
    public void DecodesTinyIntValues(byte marker, int expectedValue)
    {
        var buffer = new ReadOnlySequence<byte>([marker]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(expectedValue);
        result.BytesConsumed.Should().Be(1);
    }
}
