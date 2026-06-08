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
using System.Text;
using FluentAssertions;
using Neo4j.Driver.Bolt.PackStream.Implementations;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream;

[TestFixture]
internal class PackStreamWriterTests
{
    private static byte[] Encode(Action<PackStreamWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(new PackStreamWriter(buffer));
        return buffer.WrittenSpan.ToArray();
    }

    [Test]
    public void WriteNullMatchesSpec()
    {
        Encode(w => w.WriteNull()).Should().Equal(0xC0);
    }

    [TestCase(true, 0xC3)]
    [TestCase(false, 0xC2)]
    public void WriteBooleanEncodesMarker(bool value, byte expected)
    {
        Encode(w => w.WriteBoolean(value)).Should().Equal(expected);
    }

    [TestCase(0L, new byte[] { 0x00 })]
    [TestCase(1L, new byte[] { 0x01 })]
    [TestCase(42L, new byte[] { 0x2A })]
    [TestCase(100L, new byte[] { 0x64 })]
    [TestCase(127L, new byte[] { 0x7F })]
    [TestCase(-1L, new byte[] { 0xFF })]
    [TestCase(-16L, new byte[] { 0xF0 })]
    [TestCase(-17L, new byte[] { 0xC8, 0xEF })]
    [TestCase(-128L, new byte[] { 0xC8, 0x80 })]
    [TestCase(128L, new byte[] { 0xC9, 0x00, 0x80 })]
    [TestCase(200L, new byte[] { 0xC9, 0x00, 0xC8 })]
    [TestCase(-100L, new byte[] { 0xC8, 0x9C })]
    [TestCase(-200L, new byte[] { 0xC9, 0xFF, 0x38 })]
    [TestCase(32_767L, new byte[] { 0xC9, 0x7F, 0xFF })]
    [TestCase(-32_768L, new byte[] { 0xC9, 0x80, 0x00 })]
    [TestCase(40_000L, new byte[] { 0xCA, 0x00, 0x00, 0x9C, 0x40 })]
    [TestCase(50_000L, new byte[] { 0xCA, 0x00, 0x00, 0xC3, 0x50 })]
    [TestCase(int.MaxValue, new byte[] { 0xCA, 0x7F, 0xFF, 0xFF, 0xFF })]
    [TestCase(int.MinValue, new byte[] { 0xCA, 0x80, 0x00, 0x00, 0x00 })]
    [TestCase(10_000_000_000L, new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x02, 0x54, 0x0B, 0xE4, 0x00 })]
    public void WriteIntegerEncodesExpectedBytes(long value, byte[] expected)
    {
        Encode(w => w.WriteInteger(value)).Should().Equal(expected);
    }

    [TestCase(0.0, new byte[] { 0xC1, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [TestCase(1.0, new byte[] { 0xC1, 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [TestCase(-1.0, new byte[] { 0xC1, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [TestCase(2.5, new byte[] { 0xC1, 0x40, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    public void WriteFloat64EncodesBigEndianIeee754(double value, byte[] expected)
    {
        Encode(w => w.WriteFloat64(value)).Should().Equal(expected);
    }

    [TestCase("", new byte[] { 0x80 })]
    [TestCase("a", new byte[] { 0x81, (byte)'a' })]
    [TestCase("hi", new byte[] { 0x82, (byte)'h', (byte)'i' })]
    [TestCase("hello", new byte[] { 0x85, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o' })]
    public void WriteStringTinyEncodesLengthAndUtf8(string value, byte[] expected)
    {
        Encode(w => w.WriteString(value)).Should().Equal(expected);
    }

    [TestCase("ab", new byte[] { 0x82, (byte)'a', (byte)'b' })]
    [TestCase("x", new byte[] { 0x81, (byte)'x' })]
    public void WriteUtf8StringMatchesWriteStringForAscii(string ascii, byte[] expected)
    {
        Encode(w => w.WriteUtf8String(Encoding.UTF8.GetBytes(ascii))).Should().Equal(expected);
    }

    [TestCase(new byte[0], new byte[] { 0xCC, 0x00 })]
    [TestCase(new byte[] { 0xFF }, new byte[] { 0xCC, 0x01, 0xFF })]
    [TestCase(new byte[] { 0xAA, 0xBB }, new byte[] { 0xCC, 0x02, 0xAA, 0xBB })]
    public void WriteBytesEncodesLengthPrefix(byte[] payload, byte[] expected)
    {
        Encode(w => w.WriteBytes(payload)).Should().Equal(expected);
    }

    [Test]
    public void WriteStructHeaderTinyMatchesSpec()
    {
        Encode(w => w.WriteStructHeader(0x10, 1)).Should().Equal(0xB1, 0x10);
    }

    [Test]
    public void WriteStructHeaderStruct8MatchesSpec()
    {
        Encode(w => w.WriteStructHeader(0x2A, 20)).Should().Equal(0xDC, 0x14, 0x2A);
    }

    [Test]
    public void WriteListTinyMatchesSpec()
    {
        Encode(w => w.WriteList([1, 2, 3])).Should().Equal(0x93, 0x01, 0x02, 0x03);
    }

    [Test]
    public void WriteMapTinyMatchesSpec()
    {
        var map = new Dictionary<string, object?> { ["n"] = 1 };
        Encode(w => w.WriteMap(map)).Should().Equal(0xA1, 0x81, (byte)'n', 0x01);
    }

    [Test]
    public void WriteObjectNestedMatchesSpec()
    {
        var map = new Dictionary<string, object?> { ["a"] = new List<object?> { 1, 2 } };
        Encode(w => w.WriteMap(map)).Should().Equal(0xA1, 0x81, (byte)'a', 0x92, 0x01, 0x02);
    }

    [Test]
    public void WriteStructHeaderTooManyFieldsThrowsProtocolException()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new PackStreamWriter(buffer);
        var act = () => writer.WriteStructHeader(0x01, short.MaxValue + 1);
        act.Should().Throw<Neo4j.Driver.ProtocolException>();
    }

    [Test]
    public void WriteStringNullThrowsArgumentNullException()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new PackStreamWriter(buffer);
        var act = () => writer.WriteString(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
