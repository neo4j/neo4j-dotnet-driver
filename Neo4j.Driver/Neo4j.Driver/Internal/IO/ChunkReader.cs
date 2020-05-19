// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkReader : IChunkReader
    {
        private class StreamBuffer
        {
            private readonly byte[] _streamBuffer = new byte[Constants.ChunkBufferSize];
            private byte[] Buffer { get { return _streamBuffer; } }
            
            public byte this[int index]
            {
                get
                {
                    return _streamBuffer[index];
                }
                set
                {
                    _streamBuffer[index] = value;
                }
            }
            public int Position { get; set; } = 0;
            public int Length { get { return _streamBuffer.Length; } }
            public int Size { get; set; }
            
            public int Write(Stream inputStream, int offset = 0)
            {
                Position = offset;
                Size = inputStream.Read(Buffer, Position, Length);
                return Size;
            }

            public async Task<int> WriteAsync(Stream inputStream, int offset = 0)
            {
                Position = offset; 
                Size = await inputStream.ReadAsync(Buffer, Position, Length).ConfigureAwait(false);
                return Size;
            }

            public int Read(byte[] target, int offset, int readSize)
            {
                if (readSize <= 0) return 0;

                readSize = Math.Min(readSize, Size - Position);                
                System.Buffer.BlockCopy(Buffer, Position, target, offset, readSize);
                Position += readSize;
                return readSize;
            }

            public int Read(Stream targetStream, int readSize)
            {
                if (readSize <= 0) return 0;

                readSize = Math.Min(readSize, Size - Position);
                targetStream.Write(Buffer, Position, readSize);
                Position += readSize;
                return readSize;
            }
                        
            public void LogBuffer(ILogger logger)
            {
                if (logger != null && logger.IsTraceEnabled())
                {
                    logger?.Trace("S: {0}", Buffer.ToHexString(0, Size));
                }
            }
        }


        private Stream InputStream { get; set; }
        private ILogger Logger { get; set; }        
        private StreamBuffer DataStreamBuffer{ get; set; }
        private int RemainingReadSize { get; set; } = 0;
        private bool IsMessageOpen { get; set; } = false;
        private int MessageCount { get; set; } = 0;
        private bool DataRead { get; set; } = false;

        const int ChunkHeaderSize = 2;
        private readonly byte[] _chunkSizeBuffer = new byte[ChunkHeaderSize];


        public ChunkReader(Stream downStream)
            : this(downStream, null)
        {   
        }

        internal ChunkReader(Stream downStream, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));
            Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanRead, nameof(downStream));

            InputStream = downStream;
            Logger = logger;
            DataStreamBuffer = new StreamBuffer();
        }


        public int ReadNextMessages(Stream outputMessageStream)
        {
            var previousStreamPosition = outputMessageStream.Position;
            
            while (DataStreamBuffer.Write(InputStream) > 0)
            {
                DataRead = true;
                DataStreamBuffer.LogBuffer(Logger);
                MessageCount += ExtractMessages(outputMessageStream);
            }

            CheckEndOfStreamValidity();

            outputMessageStream.Position = previousStreamPosition;
            return MessageCount;
        }
        
        public async Task<int> ReadNextMessagesAsync(Stream outputMessageStream)
        {   
            MessageCount = 0;
            var previousStreamPosition = outputMessageStream.Position;            

            while(await DataStreamBuffer.WriteAsync(InputStream) > 0)
            {
                DataRead = true;
                DataStreamBuffer.LogBuffer(Logger);
                MessageCount += ExtractMessages(outputMessageStream);
            }

            CheckEndOfStreamValidity();

            outputMessageStream.Position = previousStreamPosition;
            return MessageCount;
        }

        
        private int ExtractMessages(Stream outputMessageStream)
        {
            int chunkSize = 0,
                previousChunkSize = 0,
                messageCount = 0;

            while (DataStreamBuffer.Position < DataStreamBuffer.Size)
            {
                previousChunkSize = chunkSize;
                chunkSize = RemainingReadSize > 0 ?  RemainingReadSize : ReadChunkSize();
                
                if (chunkSize > 0) OpenMessage();   //Zero chunksize is a NOOP so don't open a message
                
                RemainingReadSize = chunkSize - DataStreamBuffer.Read(outputMessageStream, chunkSize);

                if (previousChunkSize > 0 && chunkSize == 0)   //This is the NOOP at the end of a message
                {
                    messageCount++;
                    CloseMessage();
                }
            }
            
            return messageCount;
        }

        private int ReadChunkSize()
        {
            var sizeRead = DataStreamBuffer.Read(_chunkSizeBuffer, 0, _chunkSizeBuffer.Length);
            
            if(sizeRead < _chunkSizeBuffer.Length)
            {
                throw new IOException($"Unexpected end of stream, read returned {sizeRead}. Read less than {_chunkSizeBuffer.Length} bytes when attempting to read chunk size");
            }

            return PackStreamBitConverter.ToUInt16(_chunkSizeBuffer);            
        }

        private void CheckEndOfStreamValidity()
        {
            if(RemainingReadSize > 0)
                throw new IOException($"Unexpected end of stream, {RemainingReadSize} bytes still expected to be read");

            if (IsMessageOpen) //This is the end of the stream, and we still have an open message
                throw new IOException($"Unexpected end of stream, still have an unterminated message");

            if(!DataRead)
                throw new IOException($"Unexpected end of stream - empty stream");
        }

        private void OpenMessage()
        {
            IsMessageOpen = true;
        }

        private void CloseMessage()
        {
            IsMessageOpen = false;
        }
    }
}





