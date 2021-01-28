// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkWriter: Stream, IChunkWriter
    {
        private static readonly byte[] ZeroChunkSizeBuffer = PackStreamBitConverter.GetBytes((ushort) 0);

        private readonly int _chunkSize;
        private readonly Stream _downStream;
        private readonly MemoryStream _chunkStream;
        private readonly IDriverLogger _logger;
        private readonly int _defaultBufferSize;
        private readonly int _maxBufferSize;
        private int _shrinkCounter = 0;

        private long _startPos = -1;
        private long _dataPos = -1;

        private readonly byte[] _buffer = new byte[8 * 1024];

        public ChunkWriter(Stream downStream)
            : this(downStream, null)
        {
            
        }

        public ChunkWriter(Stream downStream, int chunkSize)
            : this(downStream, null, chunkSize)
        {

        }

        public ChunkWriter(Stream downStream, IDriverLogger logger)
            : this(downStream, logger, Constants.MaxChunkSize)
        {

        }

        public ChunkWriter(Stream downStream, int defaultBufferSize, int maxBufferSize, IDriverLogger logger)
            : this(downStream, defaultBufferSize, maxBufferSize, logger, Constants.MaxChunkSize)
        {

        }


        public ChunkWriter(Stream downStream, IDriverLogger logger, int chunkSize)
            : this(downStream, Constants.DefaultWriteBufferSize, Constants.MaxWriteBufferSize, logger, chunkSize)
        {

        }

        public ChunkWriter(Stream downStream, int defaultBufferSize, int maxBufferSize, IDriverLogger logger, int chunkSize)
        {
            Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));
            Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanWrite, nameof(downStream));
            Throw.ArgumentOutOfRangeException.IfValueLessThan(chunkSize, Constants.MinChunkSize, nameof(chunkSize));
            Throw.ArgumentOutOfRangeException.IfValueGreaterThan(chunkSize, Constants.MaxChunkSize, nameof(chunkSize));

            _logger = logger;
            _chunkSize = chunkSize;
            _downStream = downStream;
            _defaultBufferSize = defaultBufferSize;
            _maxBufferSize = maxBufferSize;
            _chunkStream = new MemoryStream(_defaultBufferSize);
        }

        public Stream ChunkerStream => this;

        public void OpenChunk()
        {
            // Emit size buffers into the buffer
            _chunkStream.Write(ZeroChunkSizeBuffer, 0, ZeroChunkSizeBuffer.Length);

            // Mark positions
            _dataPos = _chunkStream.Position;
            _startPos = _dataPos - ZeroChunkSizeBuffer.Length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var currentLength = _chunkStream.Position - _dataPos;
            var nextLength = currentLength + count;

            // Is the data exceeding our maximum chunk size?
            if (nextLength > _chunkSize)
            {
                var leftToChunk = count;
                var thisChunkIndex = offset;

                while (leftToChunk > 0)
                {
                    var thisChunkSize = (int)Math.Min(leftToChunk, _chunkSize - currentLength);

                    _chunkStream.Write(buffer, thisChunkIndex, thisChunkSize);

                    thisChunkIndex += thisChunkSize;
                    leftToChunk -= thisChunkSize;

                    currentLength = 0;

                    // If there's still more data, then close existing chunk and open a new one.
                    if (leftToChunk > 0)
                    {
                        CloseChunk();

                        OpenChunk();
                    }
                }
            }
            else
            {
                _chunkStream.Write(buffer, offset, count);
            }
        }

        public void CloseChunk()
        {
            // Fill size buffers with the actual length of the chunk.
            var count = _chunkStream.Position - _dataPos;

            if (count > 0)
            {
                var chunkSize =
                    PackStreamBitConverter.GetBytes((ushort)count);

                var previousPos = _chunkStream.Position;
                try
                {
                    _chunkStream.Position = _startPos;

                    _chunkStream.Write(chunkSize, 0, chunkSize.Length);
                }
                finally
                {
                    _chunkStream.Position = previousPos;
                }
            }
        }
        
        public void Send()
        {
            LogStream(_chunkStream);

            _chunkStream.Position = 0;
            _chunkStream.CopyTo(_downStream);

            Cleanup();
        }

        public Task SendAsync()
        {
            LogStream(_chunkStream);

            _chunkStream.Position = 0;

            return
                _chunkStream.CopyToAsync(_downStream)
                    .ContinueWith(t =>
                    {
                        Cleanup();

                        return TaskHelper.GetCompletedTask();
                    }).Unwrap();
        }

        private void Cleanup()
        {
            _chunkStream.Position = 0;
            _chunkStream.SetLength(0);
            if (_chunkStream.Capacity > _maxBufferSize)
            {
                _logger?.Info(
                    $@"Shrinking write buffers to the default write buffer size {
                            _defaultBufferSize
                        } since its size reached {
                            _chunkStream.Capacity
                        } which is larger than the maximum write buffer size {
                            _maxBufferSize
                        }. This has already occurred {
                            _shrinkCounter
                        } times for this connection.");

                _shrinkCounter += 1;

                _chunkStream.Capacity = _defaultBufferSize;
            }
        }

        private void LogStream(MemoryStream stream)
        {
            if (_logger != null && _logger.IsTraceEnabled())
            {
                var buffer = stream.ToArray();
                _logger?.Trace("C: {0}", buffer.ToHexString(0, buffer.Length));
            }
        }

        #region Stream Forwarders

        public override long Position
        {
            get => _chunkStream.Position;
            set => _chunkStream.Position = value;
        }

        public override bool CanRead => _chunkStream.CanRead;

        public override bool CanWrite => _chunkStream.CanWrite;

        public override bool CanSeek => _chunkStream.CanSeek;

        public override long Length => _chunkStream.Length;

        public override void SetLength(long value)
        {
            _chunkStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _chunkStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _chunkStream.Read(buffer, offset, count);
        }

        public override void Flush()
        {
            _chunkStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _chunkStream.FlushAsync(cancellationToken);
        }

        #endregion

    }
}
