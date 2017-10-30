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
        private readonly ILogger _logger;

        private readonly byte[] _chunkSizeBuffer = new byte[2];
        private readonly byte[] _buffer = new byte[Constants.ChunkBufferSize];
        private int _lastWritePosition = 0;
        private int _lastReadPosition = 0;
        private int _currentChunkSize = -1;

        public ChunkReader(Stream downStream)
            : this(downStream, null)
        {
            
        }

        internal ChunkReader(Stream downStream, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));
            Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanRead, nameof(downStream));

            _downStream = downStream;
            _logger = logger;
        }

        private bool TryReadOneCompleteMessageFromBuffer(Stream messageStream)
        {
            while (true)
            {
                // First try to retrieve the chunk size.
                if (_currentChunkSize == -1 && HasBytesAvailable(_chunkSizeBuffer.Length))
                {
                    _currentChunkSize = ReadChunkSize();
                    
                    // If this is the zero-length message boundary chunk, cleanup and return true.
                    if (_currentChunkSize == 0)
                    {
                        Cleanup();

                        return true;
                    }
                }

                // As long as we know the chunk size and some bytes available in the buffers, write those
                // to the target stream.
                if (_currentChunkSize != -1 && HasBytesAvailable())
                {
                    var count = Math.Min(_currentChunkSize, _lastWritePosition - _lastReadPosition);

                    CopyFromBuffer(messageStream, count);

                    _currentChunkSize -= count;

                    if (_currentChunkSize == 0)
                    {
                        Cleanup();
                    }

                    // Just reset the position trackers to not to run over our fixed size buffer.
                    if (_lastReadPosition == _lastWritePosition)
                    {
                        ResetPositions();
                    }
                }
                else
                {
                    break;
                }
            }

            // Otherwise we need some more data.
            return false;
        } 

        public int ReadNextMessages(Stream messageStream)
        {
            var messages = 0;

            var previousPosition = messageStream.Position;
            try
            {
                messageStream.Position = messageStream.Length;

                while (true)
                {
                    // Read next available bytes from the down stream and process it.
                    var read = _downStream.Read(_buffer, _lastWritePosition, _buffer.Length - _lastWritePosition);
                    if (read <= 0)
                    {
                        throw new IOException("Unexpected end of stream.");
                    }

                    _logger?.Trace("S: ", _buffer, _lastWritePosition, read);

                    _lastWritePosition += read;

                    // Can we read a whole message from what we have?
                    if (TryReadOneCompleteMessageFromBuffer(messageStream))
                    {
                        messages += 1;

                        break;
                    }
                }

                // Try to consume more messages from the left-over data in the buffer
                var readFromBuffer = TryReadOneCompleteMessageFromBuffer(messageStream);
                while (readFromBuffer)
                {
                    messages += 1;

                    readFromBuffer = TryReadOneCompleteMessageFromBuffer(messageStream);
                }
            }
            finally
            {
                messageStream.Position = previousPosition;
            }

            return messages;
        }

        
        public Task<int> ReadNextMessagesAsync(Stream messageStream)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            // Track and manage target stream's positions.
            var previousPosition = messageStream.Position;
            taskCompletionSource.Task.ContinueWith(t =>
            {
                messageStream.Position = previousPosition;
            }, TaskContinuationOptions.ExecuteSynchronously);
            messageStream.Position = messageStream.Length;

            ReadNextChunkLoopAsync(
                messageStream,
                taskCompletionSource,
                TaskExtensions.GetCompletedTask());

            return taskCompletionSource.Task;
        }

        private Task ReadNextChunkLoopAsync(Stream messageStream, TaskCompletionSource<int> taskCompletionSource,
            Task previousTask)
        {
            return previousTask.ContinueWith(pt =>
            {
                try
                {
                    return
                        // Read next available bytes from the down stream asynchronously.
                        _downStream.ReadAsync(_buffer, _lastWritePosition, _buffer.Length - _lastWritePosition)
                            .ContinueWith(t =>
                            {
                                // Is Read operation canceled?
                                if (t.IsCanceled)
                                {
                                    taskCompletionSource.SetCanceled();

                                    throw new OperationCanceledException();
                                }

                                // Is it faulted?
                                if (t.IsFaulted)
                                {
                                    taskCompletionSource.SetException(t.Exception.GetBaseException());

                                    throw new OperationCanceledException();
                                }

                                if (t.Result <= 0)
                                {
                                    throw new IOException("Unexpected end of stream.");
                                }

                                // Otherwise process it.
                                _logger?.Trace("S: ", _buffer, _lastWritePosition, t.Result);

                                _lastWritePosition += t.Result;

                                return TryReadOneCompleteMessageFromBuffer(messageStream);
                            }, TaskContinuationOptions.ExecuteSynchronously)
                            .ContinueWith(t =>
                            {
                                // Is buffer processing canceled?
                                if (t.IsCanceled)
                                {
                                    taskCompletionSource.SetCanceled();

                                    return TaskExtensions.GetCompletedTask();
                                }

                                // Is buffer processing faulted?
                                if (t.IsFaulted)
                                {
                                    var exc = t.Exception.GetBaseException();

                                    if (!(exc is OperationCanceledException))
                                    {
                                        taskCompletionSource.SetException(exc);
                                    }

                                    return TaskExtensions.GetCompletedTask();
                                }

                                // Could we read a whole message from what we have?
                                if (t.Result)
                                {
                                    var messages = 1;

                                    // Try to consume more messages from the left-over data in the buffer
                                    var readFromBuffer = TryReadOneCompleteMessageFromBuffer(messageStream);
                                    while (readFromBuffer)
                                    {
                                        messages += 1;

                                        readFromBuffer = TryReadOneCompleteMessageFromBuffer(messageStream);
                                    }

                                    // Mark the asynchronous task as completed.
                                    taskCompletionSource.SetResult(messages);

                                    return TaskExtensions.GetCompletedTask();
                                }

                                // We need some more data.
                                return ReadNextChunkLoopAsync(messageStream, taskCompletionSource, t);
                            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
                }
                catch (Exception exc)
                {
                    taskCompletionSource.SetException(exc);
                }

                return TaskExtensions.GetCompletedTask();
            });
        }

        private bool HasBytesAvailable()
        {
            return _lastWritePosition > _lastReadPosition;
        }

        private bool HasBytesAvailable(int count)
        {
            return count <= (_lastWritePosition - _lastReadPosition);
        }

        private int ReadChunkSize()
        {
            Array.ConstrainedCopy(_buffer, _lastReadPosition, _chunkSizeBuffer, 0, _chunkSizeBuffer.Length);

            _lastReadPosition += _chunkSizeBuffer.Length;
            
            return PackStreamBitConverter.ToUInt16(_chunkSizeBuffer);
        }
        
        private void CopyFromBuffer(Stream target, int count)
        {
            target.Write(_buffer, _lastReadPosition, count);

            _lastReadPosition += count;
        }

        private void Cleanup()
        {
            _currentChunkSize = -1;

            if (_lastReadPosition == _lastWritePosition)
            {
                ResetPositions();
            }
        }

        private void ResetPositions()
        {
            _lastWritePosition = 0;
            _lastReadPosition = 0;
        }
        
    }
}
