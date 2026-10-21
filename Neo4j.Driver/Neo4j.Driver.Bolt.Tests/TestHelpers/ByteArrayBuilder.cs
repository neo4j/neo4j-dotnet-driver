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

using System.Collections;
using Neo4j.Driver.Bolt.PackStream;

namespace Neo4j.Driver.Bolt.Tests.TestHelpers;

public class ByteArrayBuilder : IEnumerable<byte>
{
    public byte[] Bytes { get; }

    public ByteArrayBuilder Zeroes(int length)
    {
        return new ByteArrayBuilder(this, Enumerable.Repeat((byte)0, length));
    }

    public ByteArrayBuilder ExactBytes (IEnumerable<byte> bytes)
    {
        return new ByteArrayBuilder(this, bytes);
    }

    public ByteArrayBuilder Range(byte start, byte count)
    {
        return new ByteArrayBuilder(this, Enumerable.Range(start, count).Select(i => (byte)i));
    }

    public ByteArrayBuilder Range(Range range)
    {
        return Range((byte)range.Start.Value, (byte)(range.End.Value - range.Start.Value));
    }

    public ByteArrayBuilder PackStreamMessage(IEnumerable<byte> bytes)
    {
        var byteArray = bytes.ToArray();
        var byteArrayLength = (short)byteArray.Length;
        var messageSize = BitConverter.GetBytesBigEndian(byteArrayLength);
        return new ByteArrayBuilder(this, messageSize).ExactBytes(byteArray);
    }

    public ByteArrayBuilder() : this(null, [])
    {
    }

    private ByteArrayBuilder(ByteArrayBuilder? previous, IEnumerable<byte> bytes)
    {
        var prepend = previous ?? Enumerable.Empty<byte>();
        Bytes = prepend.Concat(bytes).ToArray();
    }
    
    public IEnumerator<byte> GetEnumerator()
    {
        return Bytes.Cast<byte>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public ByteArrayBuilder Then => this;
}
