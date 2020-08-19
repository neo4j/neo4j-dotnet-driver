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
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkReader : IChunkReader
    {
        private class StreamBuffer
        {
            private byte[] Buffer { get; } = new byte[Constants.ChunkBufferSize];

            public byte this[int index]
            {
                get
                {
                    return Buffer[index];
                }
                set
                {
                    Buffer[index] = value;
                }
            }
            public int Position { get; set; } = 0;
            public int Length { get { return Buffer.Length; } }
            public int Size { get; set; } = 0;
            public int RemainingData { get { return Size - Position; } }

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
                if (writeSize <= 0) return 0;

                writeSize = Math.Min(writeSize, Size - Position);                
                System.Buffer.BlockCopy(Buffer, Position, target, offset, writeSize);
                Position += writeSize;
                return writeSize;
            }

            public int WriteInto(Stream targetStream, int writeSize)
            {
                if (writeSize <= 0) return 0;

                writeSize = Math.Min(writeSize, Size - Position);
                targetStream.Write(Buffer, Position, writeSize);
                Position += writeSize;
                return writeSize;
            }
                        
            public void LogBuffer(ILogger logger)
            {
                if (logger != null && logger.IsTraceEnabled())
                {
                    logger?.Trace("S: {0}", Buffer.ToHexString(0, Size));
                }
            }

            public void Reset()
            {
                Size = 0;
                Position = 0;
            }
        }


        private Stream InputStream { get; set; }
        private ILogger Logger { get; set; }
        private StreamBuffer DataStreamBuffer { get; set; }
        private int RemainingMessageDataSize { get; set; } = 0;
        private bool IsMessageOpen { get; set; } = false;
        private int MessageCount { get; set; } = 0;
        private int CurrentChunkSize { get; set; } = 0;
        private bool DataProcessed { get; set; } = false;

        private const int ChunkHeaderSize = 2;
        private int ChunkBytesRead { get; set; } = 0;
        private readonly byte[] _chunkSizeBuffer = new byte[ChunkHeaderSize];

        enum ChunkType
        {     
            ZeroChunk = 0,
            NonZeroChunk = 1,
            NumChunkTypes = 2
        }


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


        private ChunkType ReadAndParseChunkSize()
        {
            CurrentChunkSize = ReadChunkSize();
                
            if (CurrentChunkSize == 0) //Either a message terminator or a NOOP.
                return ChunkType.ZeroChunk;
            
            return ChunkType.NonZeroChunk;
        }


        private bool ProcessStream(Stream outputMessageStream)
        {
            if (DataStreamBuffer.Size == 0)  //No data so stop
            {
                throw new IOException($"Unexpected end of stream, read returned 0.  RemainingMessageDataSize = {RemainingMessageDataSize}, MessageCount = {MessageCount}");
            }

            ParseMessages(outputMessageStream);

            //If we have consumed all the expected data then break...
            if (RemainingMessageDataSize == 0 && !IsMessageOpen)
                return false;

            return true;
        }


        public int ReadNextMessages(Stream outputMessageStream)
        {
            MessageCount = 0;
            RemainingMessageDataSize = 0;

            var previousStreamPosition = outputMessageStream.Position;
            outputMessageStream.Position = outputMessageStream.Length;

            try
            {   
                while(true)     
                {
                    DataStreamBuffer.ReadFrom(InputStream);  //Populate the buffer
                    if (!ProcessStream(outputMessageStream)) break;
                }

                CheckEndOfStreamValidity();
            }
            finally
            {
                outputMessageStream.Position = previousStreamPosition;
                DataStreamBuffer.Reset();
            }

            return MessageCount;
        }


        public async Task<int> ReadNextMessagesAsync(Stream outputMessageStream)
        {
            MessageCount = 0;
            RemainingMessageDataSize = 0;

            var previousStreamPosition = outputMessageStream.Position;
            outputMessageStream.Position = outputMessageStream.Length;

            try
            {   
                while (true)
                {
                    await DataStreamBuffer.ReadFromAsync(InputStream).ConfigureAwait(false);  //Populate the buffer
                    if (!ProcessStream(outputMessageStream)) break;
                }

                CheckEndOfStreamValidity();
            }
            finally
            {
                outputMessageStream.Position = previousStreamPosition;
                DataStreamBuffer.Reset();
            }

            return MessageCount;
        }


        void ParseMessages(Stream outputMessageStream)
        {
            while (DataStreamBuffer.RemainingData > 0)
            {
                if (RemainingMessageDataSize == 0)
                {
                    if (ReadAndParseChunkSize() == ChunkType.ZeroChunk)
                    {
                        if (IsMessageOpen)
                        {
                            CloseMessage();
                            MessageCount++;
                        }

                        continue;
                    }

                    OpenMessage();
                }

                var writeLength = Math.Min(RemainingMessageDataSize, DataStreamBuffer.RemainingData);
                DataStreamBuffer.WriteInto(outputMessageStream, writeLength);
                RemainingMessageDataSize -= writeLength;
            }
        }


        private int ReadChunkSize()
        {
            DataStreamBuffer.WriteInto(_chunkSizeBuffer, 0, ChunkHeaderSize);
            return PackStreamBitConverter.ToUInt16(_chunkSizeBuffer);
        }

        
        private void CheckEndOfStreamValidity()
        {
            if(DataStreamBuffer.Size > 0  &&  DataStreamBuffer.Size < ChunkHeaderSize)
                throw new IOException($"Unexpected end of stream, unable to read next chunk size");

            if (IsMessageOpen) //This is the end of the stream, and we still have an open message
                throw new IOException($"Unexpected end of stream, still have an unterminated message");
        }


        private void OpenMessage()
        {
            IsMessageOpen = true;
            RemainingMessageDataSize = CurrentChunkSize;
        }


        private void CloseMessage()
        {
            IsMessageOpen = false;
        }
    }
}


