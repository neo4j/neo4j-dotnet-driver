// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Collections.Generic;
using Neo4j.Driver.Internal.Packstream;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class ChunkedInputStream : IInputStream
    {
        private const int ChunkSize = 1024*8; 
        public static readonly byte[] Tail = {0x00, 0x00};
        private static readonly BitConverterBase BitConverter = SocketClient.BitConverter;
        private readonly Queue<byte> _chunkBuffer; 
        private readonly byte[] _headTailBuffer = new byte[2];
        private readonly ITcpSocketClient _tcpSocketClient;
        private readonly ILogger _logger;



        public ChunkedInputStream(ITcpSocketClient tcpSocketClient, ILogger logger, int? chunkSize = ChunkSize)
        {
            _tcpSocketClient = tcpSocketClient;
            _logger = logger;
            _chunkBuffer = new Queue<byte>(chunkSize.Value);
        }

        public sbyte ReadSByte()
        {
            Ensure(1);
            return (sbyte) _chunkBuffer.Dequeue();
        }

        public byte ReadByte()
        {
            Ensure(1);
            return _chunkBuffer.Dequeue();
        }

        public short ReadShort()
        {
            Ensure(2);
            return BitConverter.ToInt16(_chunkBuffer.DequeueToArray(2));
        }

        public int ReadInt()
        {
            Ensure(4);
            return BitConverter.ToInt32(_chunkBuffer.DequeueToArray(4));
        }

        public long ReadLong()
        {
            Ensure(8);

            var bytes = _chunkBuffer.DequeueToArray(8);

            return BitConverter.ToInt64(bytes);
        }

        public double ReadDouble()
        {
            Ensure(8);

            var bytes = _chunkBuffer.DequeueToArray(8);

            return BitConverter.ToDouble(bytes);
        }

        public void ReadBytes(byte[] buffer, int offset = 0, int? length = null)
        {
            if (length == null)
                length = buffer.Length;

            Ensure(length.Value);
            for (int i = 0; i < length.Value; i++)
            {
                buffer[i+offset] = _chunkBuffer.Dequeue();
            }
        }

        public byte PeekByte()
        {
            Ensure(1);
            return _chunkBuffer.Peek();
        }

        private void Ensure(int size)
        {
            while (_chunkBuffer.Count < size)
            {
                // head
                ReadSpecifiedSize(_headTailBuffer);
                var chunkSize = BitConverter.ToUInt16(_headTailBuffer);

                // chunk
                var chunk = new byte[chunkSize];
                ReadSpecifiedSize(chunk);
                for (var i = 0; i < chunkSize; i ++)
                {
                    _chunkBuffer.Enqueue(chunk[i]);
                }

            }
        }

        private void ReadSpecifiedSize(byte[] buffer)
        {
            if (buffer.Length == 0)
            {
                return;
            }
            var numberOfbytesRead = _tcpSocketClient.ReadStream.Read(buffer);
            if (numberOfbytesRead != buffer.Length)
            {
                throw new Neo4jException($"Expect {buffer.Length}, but got {numberOfbytesRead}");
            }

            _logger?.Trace("S: ", buffer, 0, buffer.Length);
        }

        public void ReadMessageTail()
        {
            // tail 00 00 
            ReadSpecifiedSize(_headTailBuffer);
            if (_headTailBuffer.Equals(Tail))
            {
                throw new Neo4jException("Not chunked correctly");
            }
        }

    }
}