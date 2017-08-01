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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkReader: IChunkReader
    {
        private readonly Stream _downStream;
        private readonly MemoryStream _chunkStream;
        private readonly ILogger _logger;
        private long _lastWritePosition = 0;
        private long _lastReadPosition = 0;

        private readonly byte[] _chunkSizeBuffer = new byte[2];
        private readonly byte[] _buffer = new byte[8 * 1024];
        private int _currentChunkSize = -1;

        public ChunkReader(Stream downStream)
            : this(downStream, null)
        {
            
        }

        public ChunkReader(Stream downStream, ILogger logger)
            : this(downStream, new MemoryStream(), logger)
        {
            
        }

        internal ChunkReader(Stream downStream, MemoryStream chunkStream, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));
            Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanRead, nameof(downStream));

            Throw.ArgumentNullException.IfNull(chunkStream, nameof(chunkStream));

            _downStream = downStream;
            _chunkStream = chunkStream;
            _logger = logger;
        }

        public void ReadNextMessage(Stream targetStream)
        {
            while (true)
            {
                // We have not received the chunk size yet, but it can be read from the buffered data.
                if (_currentChunkSize == -1 && HasBytesAvailable(_chunkSizeBuffer.Length))
                {
                    ReadFromChunkStream(_chunkSizeBuffer, 0, _chunkSizeBuffer.Length);

                    _currentChunkSize = PackStreamBitConverter.ToUInt16(_chunkSizeBuffer);
                    if (_currentChunkSize == 0)
                    {
                        break;
                    }
                }

                // We have the chunk size, and all of the chunk data can be read from the buffered data.
                if (_currentChunkSize != -1 && HasBytesAvailable(_currentChunkSize))
                {
                    CopyToFromChunkStream(targetStream, _currentChunkSize);

                    Cleanup();
                }
                else
                {
                    // Read next available bytes from the down stream and write it to our chunk stream.
                    var read = _downStream.Read(_buffer, 0, _buffer.Length);
                    WriteToChunkStream(_buffer, 0, read);
                }
            }

            Cleanup();
        }

        
        public Task ReadNextMessageAsync(Stream targetStream)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            ReadNextChunkLoopAsync(
                targetStream, 
                taskCompletionSource,
#if NET45
                Task.FromResult(0));
#else
                Task.CompletedTask);

#endif

            return taskCompletionSource.Task;
        }

        // TODO: Try to complete this step in synchronous mode
        private Task ReadNextChunkLoopAsync(Stream targetStream, TaskCompletionSource<object> taskCompletionSource, Task previousTask)
        {
            return 
                previousTask.ContinueWith(pt =>
                {
                    try
                    {
                        // We have not received the chunk size yet, but it can be read from the buffered data.
                        if (_currentChunkSize == -1 && HasBytesAvailable(_chunkSizeBuffer.Length))
                        {
                            ReadFromChunkStream(_chunkSizeBuffer, 0, _chunkSizeBuffer.Length);

                            _currentChunkSize = PackStreamBitConverter.ToUInt16(_chunkSizeBuffer);
                            if (_currentChunkSize == 0)
                            {
                                Cleanup();

                                taskCompletionSource.SetResult(null);

#if NET45
                                return Task.FromResult(0);
#else
                                return Task.CompletedTask;
#endif
                            }
                        }

                        // We have the chunk size, and all of the chunk data can be read from the buffered data.
                        if (_currentChunkSize != -1 && HasBytesAvailable(_currentChunkSize))
                        {
                            CopyToFromChunkStream(targetStream, _currentChunkSize);

                            Cleanup();

                            return ReadNextChunkLoopAsync(targetStream, taskCompletionSource, pt);
                        }
                        else
                        {
                            // Read next available bytes from the down stream and write it to our chunk stream.
                            return
                                _downStream.ReadAsync(_buffer, 0, _buffer.Length)
                                    .ContinueWith(t => WriteToChunkStream(_buffer, 0, t.Result))
                                    .ContinueWith(t => ReadNextChunkLoopAsync(targetStream, taskCompletionSource, t)).Unwrap();
                        }
                    }
                    catch (Exception exc)
                    {
                        taskCompletionSource.SetException(exc);
                    }

#if NET45
                    return Task.FromResult(0);
#else
                    return Task.CompletedTask;
#endif
                }).Unwrap();
        }

        private bool HasBytesAvailable(int count)
        {
            return count <= (_chunkStream.Length - _lastReadPosition);
        }

        private int ReadFromChunkStream(byte[] buffer, int offset, int count)
        {
            int result = 0;

            try
            {
                _chunkStream.Position = _lastReadPosition;

                int hasRead = 0, from = offset, toRead = count;
                do
                {
                    hasRead = _chunkStream.Read(buffer, from, toRead);
                    from += hasRead;
                    toRead -= hasRead;
                } while (toRead > 0 && hasRead > 0);

                result = count;
            }
            finally
            {
                _lastReadPosition = _chunkStream.Position;
            }

            return result;
        }

        private void WriteToChunkStream(byte[] buffer, int offset, int count)
        {
            try
            {
                _chunkStream.Position = _lastWritePosition;

                _chunkStream.Write(buffer, offset, count);
            }
            finally
            {
                _lastWritePosition = _chunkStream.Position;
            }

            _logger?.Trace("S: ", buffer, offset, count);
        }

        private void CopyToFromChunkStream(Stream target, int count)
        {
            try
            {
                _chunkStream.Position = _lastReadPosition;

                var toRead = count;
                while (toRead > 0)
                {
                    var read = _chunkStream.Read(_buffer, 0, Math.Min(toRead, _buffer.Length));
                    if (read > 0)
                    {
                        target.Write(_buffer, 0, read);
                        toRead -= read;
                    }
                }
            }
            finally
            {
                _lastReadPosition = _chunkStream.Position;
            }
        }

        private void Cleanup()
        {
            _currentChunkSize = -1;

            if (_chunkStream.Length > Constants.MaxChunkBufferSize)
            {
                // Shrink our chunk stream
                var shrinkFrom = Math.Min(_lastReadPosition, _lastWritePosition);
                _chunkStream.Position = shrinkFrom;

                var leftOverData = new byte[_chunkStream.Length - shrinkFrom];
                if (leftOverData.Length > 0)
                {
                    _chunkStream.Read(leftOverData);
                }

                _chunkStream.SetLength(0);
                _chunkStream.Write(leftOverData, 0, leftOverData.Length);

                _lastReadPosition -= shrinkFrom;
                _lastWritePosition -= shrinkFrom;
            }
        }

    }
}
