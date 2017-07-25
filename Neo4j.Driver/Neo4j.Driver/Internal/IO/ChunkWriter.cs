using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkWriter: IChunkWriter
    {
        private readonly Stream _downStream;
        private readonly MemoryStream _chunkStream;

        private readonly byte[] _buffer = new byte[8 * 1024];

        public ChunkWriter(Stream downStream)
        {
            Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));

            _downStream = downStream;
            _chunkStream = new MemoryStream();
        }

        public void WriteChunk(byte[] buffer, int offset, int count)
        {
            byte[] chunkSize = 
                PackStreamBitConverter.GetBytes((ushort)count);

            _chunkStream.Write(chunkSize, 0, chunkSize.Length);
            _chunkStream.Write(buffer, offset, count);
        }

        public void Flush()
        {
            _chunkStream.Position = 0;
            _chunkStream.CopyTo(_downStream);

            _chunkStream.Position = 0;
            _chunkStream.SetLength(0);
        }

        public Task FlushAsync()
        {
            _chunkStream.Position = 0;

            return
                _chunkStream.CopyToAsync(_downStream)
                    .ContinueWith(t =>
                    {
                        _chunkStream.Position = 0;
                        _chunkStream.SetLength(0);

                        return Task.CompletedTask;
                    }).Unwrap();
        }

    }
}
