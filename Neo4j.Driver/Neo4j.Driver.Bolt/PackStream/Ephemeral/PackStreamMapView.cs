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
/// A PackStream map value that supports allocation-free enumeration of key-value entries
/// via foreach, or heap-allocated enumeration via ToEnumerable() for LINQ operations.
/// </summary>
public readonly struct PackStreamMapView
{
    private readonly ReadOnlySequence<byte> _entriesData;
    private readonly int _entryCount;
    private readonly IPackStreamDecoder _decoder;

    internal PackStreamMapView(
        ReadOnlySequence<byte> entriesData,
        int entryCount,
        IPackStreamDecoder decoder)
    {
        _entriesData = entriesData;
        _entryCount = entryCount;
        _decoder = decoder;
    }

    public int Count => _entryCount;

    /// <summary>
    /// Returns an allocation-free enumerator for use with foreach.
    /// </summary>
    public Enumerator GetEnumerator() => new(_entriesData, _entryCount, _decoder);

    /// <summary>
    /// Returns a heap-allocated IEnumerable for LINQ operations.
    /// </summary>
    public IEnumerable<KeyValuePair<PackStreamValueView, PackStreamValueView>> ToEnumerable()
    {
        var remaining = _entriesData;
        for (var i = 0; i < _entryCount; i++)
        {
            if (remaining.IsEmpty)
            {
                throw new InvalidOperationException(
                    $"Unexpected end of data: expected {_entryCount} map entries but only found {i}.");
            }

            var keyResult = _decoder.Decode(remaining);
            remaining = remaining.Slice(keyResult.BytesConsumed);

            var valueResult = _decoder.Decode(remaining);
            remaining = remaining.Slice(valueResult.BytesConsumed);

            yield return new KeyValuePair<PackStreamValueView, PackStreamValueView>(keyResult.Value, valueResult.Value);
        }
    }

    public ref struct Enumerator
    {
        private readonly IPackStreamDecoder _decoder;
        private SequenceReader<byte> _reader;
        private int _remainingCount;
        private KeyValuePair<PackStreamValueView, PackStreamValueView> _current;

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

        public KeyValuePair<PackStreamValueView, PackStreamValueView> Current => _current;

        public bool MoveNext()
        {
            if (_remainingCount == 0)
            {
                return false;
            }

            if (_reader.Remaining == 0)
            {
                throw new InvalidOperationException(
                    $"Unexpected end of data: expected {_remainingCount} more map entries but no data remains.");
            }

            var keyResult = _decoder.Decode(_reader.UnreadSequence);
            _reader.Advance(keyResult.BytesConsumed);
            var valueResult = _decoder.Decode(_reader.UnreadSequence);
            _reader.Advance(valueResult.BytesConsumed);
            _current = new KeyValuePair<PackStreamValueView, PackStreamValueView>(keyResult.Value, valueResult.Value);
            _remainingCount--;
            return true;
        }
    }
}
