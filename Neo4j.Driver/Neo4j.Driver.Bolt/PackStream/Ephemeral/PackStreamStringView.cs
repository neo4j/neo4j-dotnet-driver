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

namespace Neo4j.Driver.Bolt.PackStream.Ephemeral;

/// <summary>
/// A PackStream string value that supports allocation-free enumeration of UTF-8 characters,
/// or heap-allocated string conversion via ToString().
/// </summary>
public readonly ref struct PackStreamStringView
{
    private readonly ReadOnlySequence<byte> _bytes;

    public PackStreamStringView(ReadOnlySequence<byte> bytes) => _bytes = bytes;

    public Enumerator GetEnumerator() => new(_bytes);

    public ref struct Enumerator
    {
        private SequenceReader<byte> _reader;
        private Rune _current;

        public Enumerator(ReadOnlySequence<byte> bytes) => _reader = new(bytes);

        public Rune Current => _current;

        public bool MoveNext()
        {
            if (_reader.Remaining == 0)
            {
                return false;
            }

            OperationStatus status;
            int bytesConsumed;

            if (_reader.UnreadSpan.Length >= 4)
            {
                status = Rune.DecodeFromUtf8(_reader.UnreadSpan, out _current, out bytesConsumed);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                var toCopy = (int)Math.Min(4, _reader.Remaining);
                _reader.TryCopyTo(buffer[..toCopy]);
                status = Rune.DecodeFromUtf8(buffer[..toCopy], out _current, out bytesConsumed);
            }

            if (status != OperationStatus.Done)
            {
                throw new InvalidOperationException("Invalid UTF-8 sequence");
            }

            _reader.Advance(bytesConsumed);
            return true;
        }
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(_bytes.ToArray());
    }
}
