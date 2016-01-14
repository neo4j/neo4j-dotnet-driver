//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver
{
    public interface IChunkedOutput
    {
        IChunkedOutput Write(byte b, params byte[] bytes);
        IChunkedOutput Write(byte[] bytes);
        IChunkedOutput Flush();
    }

    public class PackStreamV1ChunkedOutput : IChunkedOutput
    {
        public const int BufferSize = 1024*8;
        private readonly BitConverterBase _bitConverter;
        private readonly ITcpSocketClient _tcpSocketClient;
        private byte[] _buffer; //new byte[1024*8];
        private int _pos = -1;

        public PackStreamV1ChunkedOutput(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter)
        {
            _tcpSocketClient = tcpSocketClient;
            _bitConverter = bitConverter;
        }

        public IChunkedOutput Write(byte b, params byte[] bytes)
        {
            var bytesLength = bytes?.Length ?? 0;
            Ensure(1 + bytesLength);
            _buffer[_pos] = b;
            _pos ++;
            if (bytes != null)
            {
                bytes.CopyTo(_buffer, _pos);
                _pos += bytesLength;
            }
            return this;
        }

        public IChunkedOutput Write(byte[] bytes)
        {
            if (bytes == null)
                return this;

            var bytesLength = bytes.Length;
            Ensure(bytesLength);
            bytes.CopyTo(_buffer, _pos);
            _pos += bytesLength;
            return this;
        }

        public IChunkedOutput Flush()
        {
            WriteShort((short) (_pos - 2), _buffer, 0); // size of this chunk pos+2 or pos-2
            WriteShort(0, _buffer, _pos); // pending 00 00
            _pos += 2;
            _tcpSocketClient.WriteStream.Write(_buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Flush();
            _buffer = null;
            _pos = -1;
            return this;
        }

        private void Ensure(int size)
        {
            if (_buffer == null)
            {
                /*New _buffer - start of a new chunk*/
                _buffer = new byte[BufferSize];
                _pos = 2; // reserve two bytes for chunk header
            }

            if (_buffer.Length - _pos < size + 2) // not enough to add [size] and a chunk ending (00 00)
            {
                Flush();
            }
        }

        private void WriteShort(short num, byte[] buffer, int pos)
        {
            _bitConverter.GetBytes(num).CopyTo(buffer, pos);
        }
    }
}