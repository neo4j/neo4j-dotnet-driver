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
using Neo4j.Driver.Bolt.PackStream.Abstractions;

namespace Neo4j.Driver.Bolt.PackStream.Ephemeral;

/// <summary>
/// A PackStream list value that supports allocation-free enumeration via foreach,
/// or heap-allocated enumeration via ToEnumerable() for LINQ operations.
/// </summary>
public readonly struct PackStreamListView
{
    private readonly ReadOnlySequence<byte> _itemsData;
    private readonly int _itemCount;
    private readonly IPackStreamDecoder _decoder;

    internal PackStreamListView(
        ReadOnlySequence<byte> itemsData,
        int itemCount,
        IPackStreamDecoder decoder)
    {
        _itemsData = itemsData;
        _itemCount = itemCount;
        _decoder = decoder;
    }

    public int Count => _itemCount;

    /// <summary>
    /// Returns an allocation-free enumerator for use with foreach.
    /// </summary>
    public Enumerator GetEnumerator() => new(_itemsData, _itemCount, _decoder);

    /// <summary>
    /// Decodes and returns the element at the given index without enumerating the entire list.
    /// </summary>
    /// <param name="index">Zero-based index of the element.</param>
    /// <returns>The decoded value at that index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index is negative or not less than Count.</exception>
    public PackStreamValueView ElementAt(int index)
    {
        if (index < 0 || index >= _itemCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be in range [0, {_itemCount}).");
        }

        var remaining = _itemsData;
        for (var i = 0; i <= index; i++)
        {
            var result = _decoder.Decode(remaining);
            if (i == index)
            {
                return result.Value;
            }

            remaining = remaining.Slice(result.BytesConsumed);
        }

        throw new InvalidOperationException("Unreachable");
    }

    /// <summary>
    /// Returns a heap-allocated IEnumerable for LINQ operations.
    /// </summary>
    public IEnumerable<PackStreamValueView> ToEnumerable()
    {
        var remaining = _itemsData;
        for (var i = 0; i < _itemCount; i++)
        {
            var result = _decoder.Decode(remaining);
            var consumed = result.BytesConsumed;
            yield return result.Value;
            remaining = remaining.Slice(consumed);
        }
    }

    public ref struct Enumerator
    {
        private readonly IPackStreamDecoder _decoder;
        private SequenceReader<byte> _reader;
        private int _remainingCount;
        private PackStreamValueView _current;

        internal Enumerator(
            ReadOnlySequence<byte> data,
            int count,
            IPackStreamDecoder decoder)
        {
            _reader = new SequenceReader<byte>(data);
            _remainingCount = count;
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            _current = default;
        }

        public PackStreamValueView Current => _current;

        public bool MoveNext()
        {
            if (_remainingCount == 0)
            {
                return false;
            }

            var result = _decoder.Decode(_reader.UnreadSequence);
            _current = result.Value;
            _reader.Advance(result.BytesConsumed);
            _remainingCount--;
            return true;
        }
    }
}
