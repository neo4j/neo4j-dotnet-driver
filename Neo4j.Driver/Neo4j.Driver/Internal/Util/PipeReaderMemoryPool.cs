// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Buffers;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Util;

/// <summary>
/// Simple memory pool based on the .NET's Pool.
/// </summary>
internal sealed class PipeReaderMemoryPool : MemoryPool<byte>
{
    private readonly int _defaultSize;
    private readonly ArrayPool<byte> _pool;
    
    public PipeReaderMemoryPool(int defaultBufferSize, int maxPooledBufferSize)
    {
        _defaultSize = defaultBufferSize;
        _pool = ArrayPool<byte>.Create(maxPooledBufferSize, 64);
    }

    public override int MaxBufferSize => int.MaxValue;

    public override IMemoryOwner<byte> Rent(int minimumBufferSize = -1)
    {
        if (minimumBufferSize == -1)
        {
            minimumBufferSize = _defaultSize;
        }
        
        if (minimumBufferSize < 0 || minimumBufferSize > MaxBufferSize)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumBufferSize), minimumBufferSize, "requested size is invalid");
        }
        
        return new PooledMemory(minimumBufferSize, _pool);
    }

    protected override void Dispose(bool disposing)
    {
    }

    private sealed class PooledMemory : IMemoryOwner<byte>
    {
        private byte[] _array;
        private readonly ArrayPool<byte> _pool;

        public PooledMemory(int size, ArrayPool<byte> pool)
        {
            _array = pool.Rent(size);
            _pool = pool;
        }

        public Memory<byte> Memory
        {
            get
            {
                var array = _array;
                if (array == null)
                {
                    throw new ObjectDisposedException(nameof(PooledMemory));
                }

                return new Memory<byte>(array);
            }
        }

        public void Dispose()
        {
            var array = _array;
            
            if (array == null)
            {
                return;
            }

            _array = null;
            _pool.Return(array);
        }
    }
}
