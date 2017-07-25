using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO
{
    internal interface IChunkWriter
    {

        void WriteChunk(byte[] buffer, int offset, int count);

        void Flush();

        Task FlushAsync();

    }
}
