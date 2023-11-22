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

using System;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class StreamBuffer
{
    private const int BufferSize = 1024; //1k.
    private byte[] Buffer { get; } = new byte[BufferSize];

    public byte this[int index]
    {
        get => Buffer[index];
        set => Buffer[index] = value;
    }

    public int Position { get; set; }
    public int Length => Buffer.Length;
    public int Size { get; set; }
    public int RemainingData => Size - Position;

    public int ReadFrom(Stream inputStream, int offset = 0)
    {
        Position = 0;
        Size = inputStream.Read(Buffer, offset, Length - offset);
        return Size;
    }

    public async Task<int> ReadFromAsync(Stream inputStream, int offset = 0)
    {
        Position = 0;
        Size = await inputStream.ReadAsync(Buffer, offset, Length - offset).ConfigureAwait(false);
        return Size;
    }

    public int WriteInto(byte[] target, int offset, int writeSize)
    {
        if (writeSize <= 0)
        {
            return 0;
        }

        writeSize = Math.Min(writeSize, Size - Position);
        System.Buffer.BlockCopy(Buffer, Position, target, offset, writeSize);
        Position += writeSize;
        return writeSize;
    }

    public int WriteInto(Stream targetStream, int writeSize)
    {
        if (writeSize <= 0)
        {
            return 0;
        }

        writeSize = Math.Min(writeSize, Size - Position);
        targetStream.Write(Buffer, Position, writeSize);
        Position += writeSize;
        return writeSize;
    }

    public void Reset()
    {
        Size = 0;
        Position = 0;
    }
}
