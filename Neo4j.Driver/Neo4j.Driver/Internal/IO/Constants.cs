using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4j.Driver.Internal.IO
{
    internal static class Constants
    {
        public const int BufferSize = 8 * 1024;
        public const int MaxChunkSize = (64 * 1024) - 1;
        public const int MaxChunkBufferSize = MaxChunkSize;
    }
}
