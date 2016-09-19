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

namespace Neo4j.Driver.Internal.Connector
{
    internal class ChunkedOutputStream : IOutputStream
    {
        internal const int BufferSize = 1024*8;
        private const int ChunkHeaderBufferSize = 2;
        private readonly int _chunkSize;
        private static readonly BitConverterBase BitConverter = SocketClient.BitConverter;
        private readonly ITcpSocketClient _tcpSocketClient;
        private byte[] _buffer; //new byte[1024*8];
        private int _pos = -1;
        private int _chunkLength = -1;
        private int _chunkHeaderPosition = -1;
        private bool _isInChunk = false;
        private readonly ILogger _logger;

        public ChunkedOutputStream(ITcpSocketClient tcpSocketClient, ILogger logger, int? chunkSize = BufferSize)
        {
            _tcpSocketClient = tcpSocketClient;
            _logger = logger;
            Throw.ArgumentOutOfRangeException.IfValueLessThan(chunkSize.Value, 8, nameof(chunkSize));
            Throw.ArgumentOutOfRangeException.IfValueGreaterThan(chunkSize.Value, ushort.MaxValue + 2, nameof(chunkSize));

            _chunkSize = chunkSize.Value;
        }

        public IOutputStream Write(byte b, params byte[] bytes)
        {
            // Ensure there is an open chunk, and the space is enough for a byte to write
            Ensure(1);
            WriteBytesInChunk(new[] {b});
            Write(bytes);
            return this;
        }

        public IOutputStream Write(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return this;

            var bytesLength = bytes.Length;
            var sentSize = 0;
            while (sentSize < bytes.Length)
            {
                // Ensure there is an open chunk, and that it has at least one byte of space left
                Ensure(1);
                var sizeToSend = Math.Min(_chunkSize - _pos, bytesLength-sentSize);
                WriteBytesInChunk(bytes, sentSize, sizeToSend);
                sentSize += sizeToSend;
            }
            return this;
        }

        public IOutputStream Flush()
        {
            if (_isInChunk)
            {
                WriteUShortInChunkHeader((ushort)_chunkLength);
                CloseChunk();
            }
            // else means WriteMessageTail has close the chunk with 0s

            _logger?.Trace("C: ", _buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Write(_buffer, 0, _pos);
            _tcpSocketClient.WriteStream.Flush();
            
            DisposeBuffer();

            return this;
        }

        public IOutputStream WriteMessageTail()
        {
            // finish the previous open chunk
            if (_isInChunk)
            {
                WriteUShortInChunkHeader((ushort) _chunkLength);
                CloseChunk();
            }
            // else means that the previous chunk has been flushed

            // write 00 00, which is basically is a chunk that has 0 size
            Ensure(0); // Ensure there is an open chunk with guarantee that there is space to write 00 00
            WriteUShortInChunkHeader(0); // pending 00 00
            CloseChunk();

            return this;
        }

        private void Ensure(int size)
        {
            var maxChunkSize = _chunkSize - ChunkHeaderBufferSize;
            if (size > maxChunkSize)
                Throw.ArgumentOutOfRangeException.IfValueGreaterThan(size, maxChunkSize, nameof(size));

            var toWriteSize = _isInChunk ? size : size + ChunkHeaderBufferSize;
            var bufferRemaining = _chunkSize - _pos;
            if (toWriteSize > bufferRemaining)
            {
                Flush();
            }

            if (_buffer == null)
            {
                /*New buffer and mark the start of a new chunk*/
                NewBuffer();
                OpenChunk();
            }
            else if (!_isInChunk) // just finish a message but still could write more in this chunk
            {
                OpenChunk();
            }
        }

        private void WriteBytesInChunk(byte[] bytes, int offset = 0, int? length = null)
        {
            Throw.ArgumentException.IfNotTrue(_isInChunk, nameof(_isInChunk));
            Array.Copy(bytes, offset, _buffer, _pos, length ?? bytes.Length);
            _pos += length ?? bytes.Length;
            _chunkLength += length ?? bytes.Length;
        }

        private void WriteUShortInChunkHeader(ushort num)
        {
            BitConverter.GetBytes(num).CopyTo(_buffer, _chunkHeaderPosition);
        }

        private void CloseChunk()
        {
            _isInChunk = false;
            _chunkLength = -1;
            _chunkHeaderPosition = -1;
        }

        private void OpenChunk()
        {
            _chunkLength = 0;
            _chunkHeaderPosition = _pos;

            // reserve two bytes for chunk header
            _pos += 2;

            _isInChunk = true;
        }

        private void NewBuffer()
        {
            _buffer = new byte[_chunkSize];
            _pos = 0;
        }

        private void DisposeBuffer()
        {
            _buffer = null;
            _pos = -1;
        }

    }
}