// Copyright (c) 2002-2016 "Neo Technology,"
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
using System;
using Neo4j.Driver.Internal.Packstream;
using Neo4j.Driver.V1;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver.Internal.Connector
{
    internal class ChunkedOutputStream : IOutputStream
    {
        internal const int BufferSize = 1024*8;
        private readonly int _chunkSize;
        private readonly BitConverterBase _bitConverter;
        private readonly ITcpSocketClient _tcpSocketClient;
        private byte[] _buffer; //new byte[1024*8];
        private int _pos = -1;
        private int _chunkLength = 0;
        private int _chunkHeaderPosition = 0;
        private bool _isInChunk = false;
        private readonly ILogger _logger;

        public ChunkedOutputStream(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter, ILogger logger, int? chunkSize = BufferSize)
        {
            _tcpSocketClient = tcpSocketClient;
            _bitConverter = bitConverter;
            _logger = logger;
            Throw.ArgumentOutOfRangeException.IfValueLessThan(chunkSize.Value, 8, nameof(chunkSize));

            _chunkSize = chunkSize.Value;
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
            var sentSize = 0;
            while (sentSize < bytes.Length)
            {
                var sizeToSend = Math.Min(_chunkSize - 4, bytesLength-sentSize);
                Ensure(sizeToSend);
                WriteBytes(bytes, sentSize, sizeToSend);
                sentSize += sizeToSend;
            }
            return this;
        }


        public IOutputStream Flush()
        {
            WriteShort((short)(_chunkLength), _buffer, _chunkHeaderPosition); // size of this chunk pos-2
            CloseChunk();
            _chunkHeaderPosition = 0;
            _logger?.Trace("C: ", _buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Write(_buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Flush();
            _buffer = null;
            _pos = -1;
            
            return this;
        }

        public IOutputStream WriteMessageTail()
        {
            WriteShort((short)(_chunkLength), _buffer, _chunkHeaderPosition);
            WriteShort(0, _buffer, _pos); // pending 00 00

            _pos += 2;
            _chunkHeaderPosition = _pos;
            CloseChunk();
            return this;
        }

        private void CloseChunk()
        {
            _chunkLength = 0;
            _isInChunk = false;
        }

        private void WriteBytes(byte[] bytes, int offset = 0, int? length = null)
        {
            _isInChunk = true;
            Array.Copy(bytes, offset, _buffer, _pos, length??bytes.Length);
           // bytes.CopyTo(_buffer, _pos);
            _pos += length??bytes.Length;
            _chunkLength += length ?? bytes.Length;
        }

        private void Ensure(int size)
        {
            if (size >= _chunkSize)
                return;

            if (_buffer == null)
            {
                /*New _buffer - start of a new chunk*/
                _buffer = new byte[_chunkSize];
                _pos = 2; // reserve two bytes for chunk header
            }
            else if (!_isInChunk)
            {
                _pos += 2;
            }

            if (_buffer.Length - _pos < size + 2) // not enough to add [size] and a message ending (00 00)
            {
                Flush();
                Ensure(size);
            }
        }

        private void WriteShort(short num, byte[] buffer, int pos)
        {
            _bitConverter.GetBytes(num).CopyTo(buffer, pos);
        }
    }
}