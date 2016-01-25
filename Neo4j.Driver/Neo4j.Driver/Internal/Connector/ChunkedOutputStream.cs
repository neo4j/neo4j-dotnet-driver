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

using System.Collections.Generic;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver
{
    public class ChunkedOutputStream : IOutputStream
    {
        public const int BufferSize = 1024*8;
        private readonly BitConverterBase _bitConverter;
        private readonly ITcpSocketClient _tcpSocketClient;
        private byte[] _buffer; //new byte[1024*8];
        private int _pos = -1;
        private int _chunkLength = 0;
        private int _chunkHeaderPosition = 0;
        private bool _isInChunk = false;

        public ChunkedOutputStream(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter)
        {
            _tcpSocketClient = tcpSocketClient;
            _bitConverter = bitConverter;
        }

        public IOutputStream Write(byte b, params byte[] bytes)
        {
            var bytesLength = bytes?.Length ?? 0;
            Ensure(1 + bytesLength);
            WriteBytes(new[] { b});
            if (bytes != null)
            {
                WriteBytes(bytes);
            }
            return this;
        }

        public IOutputStream Write(byte[] bytes)
        {
            if (bytes == null)
                return this;

            var bytesLength = bytes.Length;
            Ensure(bytesLength);
            WriteBytes(bytes);
            return this;
        }

        // TODO move to somewhere
        private static string ToHexString(byte[] bytes, int offset, int length)
        {
            List<string> hexes = new List<string>();
            for(int i = offset; i < offset + length; i ++)
            {
                hexes.Add(bytes[i].ToString("X2"));
            }
            return string.Join(" ", hexes);
        }

        public IOutputStream Flush()
        {
            var hex = ToHexString(_buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Write(_buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Flush();
            _buffer = null;
            _pos = -1;
            _chunkHeaderPosition = 0;
            return this;
        }

        public IOutputStream WriteMessageTail()
        {
            WriteShort((short)(_chunkLength), _buffer, _chunkHeaderPosition); // size of this chunk pos-2
            WriteShort(0, _buffer, _pos); // pending 00 00

            _pos += 2;
            EndChunk();
            return this;
        }

        private void EndChunk()
        {
            _chunkLength = 0;
            _chunkHeaderPosition = _pos;
            _isInChunk = false;
        }

        private void WriteBytes(byte[] bytes)
        {
            _isInChunk = true;
            bytes.CopyTo(_buffer, _pos);
            _pos += bytes.Length;
            _chunkLength += bytes.Length;
        }

        private void Ensure(int size)
        {
            if (_buffer == null)
            {
                /*New _buffer - start of a new chunk*/
                _buffer = new byte[BufferSize];
                _pos = 2; // reserve two bytes for chunk header
            }
            else if (!_isInChunk)
            {
                _pos += 2;
            }

            if (_buffer.Length - _pos < size + 2) // not enough to add [size] and a message ending (00 00)
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