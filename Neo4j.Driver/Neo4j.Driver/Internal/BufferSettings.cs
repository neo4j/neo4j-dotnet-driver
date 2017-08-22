using System;
using System.Collections.Generic;
using System.Text;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class BufferSettings
    {

        public BufferSettings(Config config)
            : this(config.DefaultReadBufferSize, config.MaxReadBufferSize, config.DefaultWriteBufferSize, config.MaxWriteBufferSize)
        {

        }

        public BufferSettings(int defaultReadBufferSize, int maxReadBufferSize, int defaultWriteBufferSize, int maxWriteBufferSize)
        {
            Throw.ArgumentOutOfRangeException.IfValueLessThan(defaultReadBufferSize, 0, nameof(defaultReadBufferSize));
            Throw.ArgumentOutOfRangeException.IfValueLessThan(maxReadBufferSize, 0, nameof(maxReadBufferSize));
            Throw.ArgumentOutOfRangeException.IfValueLessThan(defaultWriteBufferSize, 0, nameof(defaultWriteBufferSize));
            Throw.ArgumentOutOfRangeException.IfValueLessThan(maxWriteBufferSize, 0, nameof(maxWriteBufferSize));

            DefaultReadBufferSize = defaultReadBufferSize;
            MaxReadBufferSize = maxReadBufferSize;
            DefaultWriteBufferSize = defaultWriteBufferSize;
            MaxWriteBufferSize = maxWriteBufferSize;
        }

        public int DefaultReadBufferSize { get; }

        public int MaxReadBufferSize { get; }

        public int DefaultWriteBufferSize { get; }

        public int MaxWriteBufferSize { get; }
    }
}
