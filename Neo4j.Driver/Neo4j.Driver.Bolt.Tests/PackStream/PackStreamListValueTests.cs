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
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream;

[TestFixture]
internal class PackStreamListViewTests
{
    [Test]
    public void CountReturnsItemCount()
    {
        var decoder = new SingleByteIntDecoder();
        var listValue = new PackStreamListView(
            ReadOnlySequence<byte>.Empty,
            5,
            decoder);

        listValue.Count.Should().Be(5);
    }

    [Test]
    public void CountReturnsZeroForEmptyList()
    {
        var decoder = new SingleByteIntDecoder();
        var listValue = new PackStreamListView(
            ReadOnlySequence<byte>.Empty,
            0,
            decoder);

        listValue.Count.Should().Be(0);
    }

    [Test]
    public void ForeachEnumeratesAllItems()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x01, 0x02, 0x03]);
        var listValue = new PackStreamListView(data, 3, decoder);

        var items = new List<long>();
        foreach (var item in listValue)
        {
            items.Add(item.IntValue);
        }

        items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Test]
    public void ForeachOnEmptyListYieldsNoItems()
    {
        var decoder = new SingleByteIntDecoder();
        var listValue = new PackStreamListView(
            ReadOnlySequence<byte>.Empty,
            0,
            decoder);

        var items = new List<PackStreamValueView>();
        foreach (var item in listValue)
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }

    [Test]
    public void ForeachCanBeCalledMultipleTimes()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x01, 0x02]);
        var listValue = new PackStreamListView(data, 2, decoder);

        var firstPass = new List<long>();
        foreach (var item in listValue)
        {
            firstPass.Add(item.IntValue);
        }

        var secondPass = new List<long>();
        foreach (var item in listValue)
        {
            secondPass.Add(item.IntValue);
        }

        firstPass.Should().BeEquivalentTo([1, 2]);
        secondPass.Should().BeEquivalentTo([1, 2]);
    }

    [Test]
    public void ToEnumerableReturnsAllItems()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x01, 0x02, 0x03]);
        var listValue = new PackStreamListView(data, 3, decoder);

        var items = listValue.ToEnumerable().Select(v => v.IntValue).ToList();

        items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Test]
    public void ToEnumerableSupportsLinqOperations()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x01, 0x02, 0x03, 0x04, 0x05]);
        var listValue = new PackStreamListView(data, 5, decoder);

        var sum = listValue.ToEnumerable().Sum(v => v.IntValue);
        var filtered = listValue.ToEnumerable().Where(v => v.IntValue > 2).Select(v => v.IntValue).ToList();

        sum.Should().Be(15);
        filtered.Should().BeEquivalentTo([3, 4, 5]);
    }

    [Test]
    public void ToEnumerableOnEmptyListReturnsEmptyEnumerable()
    {
        var decoder = new SingleByteIntDecoder();
        var listValue = new PackStreamListView(
            ReadOnlySequence<byte>.Empty,
            0,
            decoder);

        listValue.ToEnumerable().Should().BeEmpty();
    }

    [Test]
    public void ToEnumerableCanBeCalledMultipleTimes()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x01, 0x02]);
        var listValue = new PackStreamListView(data, 2, decoder);

        var firstPass = listValue.ToEnumerable().Select(v => v.IntValue).ToList();
        var secondPass = listValue.ToEnumerable().Select(v => v.IntValue).ToList();

        firstPass.Should().BeEquivalentTo([1, 2]);
        secondPass.Should().BeEquivalentTo([1, 2]);
    }

    [Test]
    public void ElementAtReturnsValueAtIndex()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x0A, 0x0B, 0x0C]);
        var listValue = new PackStreamListView(data, 3, decoder);

        listValue.ElementAt(0).IntValue.Should().Be(0x0A);
        listValue.ElementAt(1).IntValue.Should().Be(0x0B);
        listValue.ElementAt(2).IntValue.Should().Be(0x0C);
    }

    [Test]
    public void ElementAtThrowsWhenIndexNegative()
    {
        var decoder = new SingleByteIntDecoder();
        var listValue = new PackStreamListView(ReadOnlySequence<byte>.Empty, 1, decoder);

        var act = () => listValue.ElementAt(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ElementAtThrowsWhenIndexGreaterOrEqualCount()
    {
        var decoder = new SingleByteIntDecoder();
        var data = new ReadOnlySequence<byte>([0x01]);
        var listValue = new PackStreamListView(data, 1, decoder);

        var act0 = () => listValue.ElementAt(1);
        var act1 = () => listValue.ElementAt(2);

        act0.Should().Throw<ArgumentOutOfRangeException>();
        act1.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Decodes each byte as an integer value, consuming 1 byte per item.
    /// </summary>
    private class SingleByteIntDecoder : IPackStreamDecoder
    {
        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
        {
            var value = buffer.FirstSpan[0];
            return new ValueDecoderResult(PackStreamValueView.Integer(value), 1);
        }

        public IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a valid value but claims zero bytes consumed (error case).
    /// </summary>
    private class ZeroBytesConsumedDecoder : IPackStreamDecoder
    {
        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer) =>
            new(PackStreamValueView.Integer(1), 0);

        public IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a valid value but claims to consume more bytes than available (error case).
    /// </summary>
    private class ExcessiveBytesConsumedDecoder : IPackStreamDecoder
    {
        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer) =>
            new(PackStreamValueView.Integer(1), 100);

        public IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}









