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
using System.Threading.Tasks;
using System.Net.Sockets;
using Neo4j.Driver;
using System.Diagnostics;
using System.Text;

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
            
            public bool CheckStreamHasData(Stream inputStream)
            {
                if (inputStream.CanSeek)
                {
                    if (inputStream.Length <= 0)
                        throw new IOException($"Unexpected end of stream - empty stream");

                    if (inputStream.Position >= inputStream.Length)
                        return false;
                }
                else
                {
                    if (inputStream is NetworkStream networkStream)
                    {
                        return networkStream.DataAvailable;
                    }
                    else
                    {   
                        throw new IOException($"Incompatible stream of type {inputStream.GetType()}");
                    }
                }
                
                return true;                
            }


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
        private int RemainingReadSize { get; set; } = 0;
        private bool IsMessageOpen { get; set; } = false;
        private int MessageCount { get; set; } = 0;
        private int CurrentChunkSize { get; set; } = 0;
        private bool DataProcessed { get; set; } = false;

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
            MessageCount = 0;
            var previousStreamPosition = outputMessageStream.Position;
            outputMessageStream.Position = outputMessageStream.Length;

            try
            {
                do
                {
                    DataStreamBuffer.ReadFrom(InputStream);
                    DataStreamBuffer.LogBuffer(Logger);

                    if (ExtractMessages(outputMessageStream, out int count))
                        MessageCount += count;
                    else
                        break;
                }
                while (DataStreamBuffer.CheckStreamHasData(InputStream) || IsMessageOpen);

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
            var previousStreamPosition = outputMessageStream.Position;
            outputMessageStream.Position = outputMessageStream.Length;

            try
            {
                do
                {   
                    await DataStreamBuffer.ReadFromAsync(InputStream).ConfigureAwait(false);

                    DataStreamBuffer.LogBuffer(Logger);
                    
                    if (ExtractMessages(outputMessageStream, out int count))
                        MessageCount += count;
                    else
                        break;
                }
                while (DataStreamBuffer.CheckStreamHasData(InputStream) || IsMessageOpen);

                CheckEndOfStreamValidity();
            }
            finally
            {
                outputMessageStream.Position = previousStreamPosition;
                DataStreamBuffer.Reset();
            }

            return MessageCount;
        }

        
        private bool ExtractMessages(Stream outputMessageStream, out int count)
        {
            int previousChunkSize = 0;
            DataProcessed = false;
            count = 0;

            while (DataStreamBuffer.Position < DataStreamBuffer.Size)
            {
                DataProcessed = true;
                previousChunkSize = CurrentChunkSize;
                CurrentChunkSize = RemainingReadSize > 0 ?  RemainingReadSize : ReadChunkSize();

                if (CurrentChunkSize > 0) OpenMessage();   //Zero chunksize is a NOOP so don't open a message
                
                RemainingReadSize = CurrentChunkSize - DataStreamBuffer.WriteInto(outputMessageStream, CurrentChunkSize);

                if (previousChunkSize > 0 && CurrentChunkSize == 0)   //This is the NOOP at the end of a message
                {
                    count++;
                    CloseMessage();
                }
            }
                        
            return DataProcessed;
        }


        private int ReadChunkSize()
        {
            var sizeRead = DataStreamBuffer.WriteInto(_chunkSizeBuffer, 0, _chunkSizeBuffer.Length);
            
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


