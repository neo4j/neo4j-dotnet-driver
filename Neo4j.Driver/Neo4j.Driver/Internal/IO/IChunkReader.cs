using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO
{
    internal interface IChunkReader
    {

        void ReadNextChunk(Stream target);

        Task ReadNextChunkAsync(Stream target);

    }
}
