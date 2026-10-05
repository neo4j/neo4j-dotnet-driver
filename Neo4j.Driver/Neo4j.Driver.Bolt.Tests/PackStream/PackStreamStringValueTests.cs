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
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream;

[TestFixture]
public class PackStreamStringViewTests
{
    [Test]
    public void EnumeratesAsciiString()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().HaveCount(5);
        result.Select(r => r.ToString()).Should().BeEquivalentTo(["h", "e", "l", "l", "o"]);
    }

    [Test]
    public void EnumeratesEmptySequence()
    {
        var sequence = ReadOnlySequence<byte>.Empty;
        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().BeEmpty();
    }

    [Test]
    public void EnumeratesMultiByteCharacters()
    {
        // "café" - é is 2 bytes in UTF-8
        var bytes = Encoding.UTF8.GetBytes("café");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().HaveCount(4);
        result.Select(r => r.ToString()).Should().BeEquivalentTo(["c", "a", "f", "é"]);
    }

    [Test]
    public void EnumeratesThreeByteCharacters()
    {
        // "日本" - each character is 3 bytes in UTF-8
        var bytes = Encoding.UTF8.GetBytes("日本");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().HaveCount(2);
        result.Select(r => r.ToString()).Should().BeEquivalentTo(["日", "本"]);
    }

    [Test]
    public void EnumeratesFourByteCharacters()
    {
        // "😀" - emoji is 4 bytes in UTF-8
        var bytes = Encoding.UTF8.GetBytes("😀");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().HaveCount(1);
        result.First().ToString().Should().Be("😀");
    }

    [Test]
    public void EnumeratesMixedCharacters()
    {
        // Mix of 1, 2, 3, and 4 byte characters
        var bytes = Encoding.UTF8.GetBytes("a é 日 😀");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().HaveCount(7); // a, space, é, space, 日, space, 😀
        string.Concat(result.Select(r => r.ToString())).Should().Be("a é 日 😀");
    }

    [Test]
    public void ThrowsOnInvalidUtf8()
    {
        // Invalid UTF-8 sequence: continuation byte without start byte
        var bytes = new byte[] { 0x80 };
        var sequence = new ReadOnlySequence<byte>(bytes);

        Assert.Throws<InvalidOperationException>(() =>
        {
            var enumerator = new PackStreamStringView(sequence);
            foreach (var _ in enumerator) { }
        });
    }

    [Test]
    public void EnumeratesMultiSegmentSequence()
    {
        // Split "café" across two segments, with é split across the boundary
        // café in UTF-8: 63 61 66 C3 A9 (c=63, a=61, f=66, é=C3 A9)
        var segment1 = new byte[] { 0x63, 0x61, 0x66, 0xC3 }; // "caf" + first byte of é
        var segment2 = new byte[] { 0xA9 }; // second byte of é

        var first = new TestSequenceSegment(segment1);
        var second = first.Append(segment2);
        var sequence = new ReadOnlySequence<byte>(first, 0, second, second.Memory.Length);

        var enumerator = new PackStreamStringView(sequence);

        var result = new List<Rune>();
        foreach (var rune in enumerator)
        {
            result.Add(rune);
        }

        result.Should().HaveCount(4);
        result.Select(r => r.ToString()).Should().BeEquivalentTo(["c", "a", "f", "é"]);
    }

    [Test]
    public void ToStringReturnsAsciiString()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        enumerator.ToString().Should().Be("hello");
    }

    [Test]
    public void ToStringReturnsEmptyStringForEmptySequence()
    {
        var sequence = ReadOnlySequence<byte>.Empty;
        var enumerator = new PackStreamStringView(sequence);

        enumerator.ToString().Should().Be("");
    }

    [Test]
    public void ToStringReturnsMultiByteCharacters()
    {
        var bytes = Encoding.UTF8.GetBytes("café");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        enumerator.ToString().Should().Be("café");
    }

    [Test]
    public void ToStringReturnsMixedCharacters()
    {
        var bytes = Encoding.UTF8.GetBytes("a é 日 😀");
        var sequence = new ReadOnlySequence<byte>(bytes);
        var enumerator = new PackStreamStringView(sequence);

        enumerator.ToString().Should().Be("a é 日 😀");
    }

    [Test]
    public void ToStringWorksOnMultiSegmentSequence()
    {
        // Split "café" across two segments
        var segment1 = new byte[] { 0x63, 0x61, 0x66, 0xC3 }; // "caf" + first byte of é
        var segment2 = new byte[] { 0xA9 }; // second byte of é

        var first = new TestSequenceSegment(segment1);
        var second = first.Append(segment2);
        var sequence = new ReadOnlySequence<byte>(first, 0, second, second.Memory.Length);

        var enumerator = new PackStreamStringView(sequence);

        enumerator.ToString().Should().Be("café");
    }

    private class TestSequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public TestSequenceSegment(byte[] memory)
        {
            Memory = memory;
        }

        public TestSequenceSegment Append(byte[] memory)
        {
            var segment = new TestSequenceSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
