using System;
using System.Collections.Generic;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver
{
    public interface IChunkedInput
    {
        sbyte ReadSByte();
        byte ReadByte();
        short ReadShort();
        int ReadInt();
        void ReadBytes(byte[] buffer, int size = 0, int? length = null);
        byte PeekByte();
    }

    internal class PackStreamV1ChunkedInput : IChunkedInput
    {
        private const int ChunkSize = 1024*8; // TODO: 2 * chunk_size of server
        public static byte[] Tail = {0x00, 0x00};
        private readonly BitConverterBase _bitConverter;
        private readonly Queue<byte> _chunkBuffer = new Queue<byte>(ChunkSize); // TODO
        private readonly byte[] _headTailBuffer = new byte[2];
        private readonly ITcpSocketClient _tcpSocketClient;

        public PackStreamV1ChunkedInput(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter)
        {
            _tcpSocketClient = tcpSocketClient;
            _bitConverter = bitConverter;
        }

        public sbyte ReadSByte()
        {
            Ensure(1);
            return (sbyte) _chunkBuffer.Dequeue();
        }

        public byte ReadByte()
        {
            Ensure(1);
            return _chunkBuffer.Dequeue();
        }

        public short ReadShort()
        {
            throw new NotImplementedException();
        }

        public int ReadInt()
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(byte[] buffer, int offset = 0, int? length = null)
        {
            if (length == null)
                length = buffer.Length;

            Ensure(length.Value);
            for (int i = 0; i < length.Value; i++)
            {
                buffer[i+offset] = _chunkBuffer.Dequeue();
            }
        }

        public byte PeekByte()
        {
            Ensure(1);
            return _chunkBuffer.Peek();
        }

        private void Ensure(int size)
        {
            while (_chunkBuffer.Count < size)
            {
                // head
                ReadSpecifiedSize(_headTailBuffer);

                var chunkSize = _bitConverter.ToInt16(_headTailBuffer);

                // chunk
                var chunk = new byte[chunkSize]; // TODO use a single buffer somehow
                ReadSpecifiedSize(chunk);
                for (var i = 0; i < chunkSize; i ++)
                {
                    _chunkBuffer.Enqueue(chunk[i]);
                }

                // tail 00 00 
                ReadSpecifiedSize(_headTailBuffer);
                if (_headTailBuffer.Equals(Tail))
                {
                    //TODO: Convert to Neo4j Exception.
                    throw new InvalidOperationException("Not chunked correctly");
                }
            }
        }

        private void ReadSpecifiedSize(byte[] buffer)
        {
            var numberOfbytesRead = _tcpSocketClient.ReadStream.Read(buffer);
            if (numberOfbytesRead != buffer.Length)
            {
                //TODO: Convert to Neo4j Exception.
                throw new InvalidOperationException($"Expect {buffer.Length}, but got {numberOfbytesRead}");
            }
        }
    }
}