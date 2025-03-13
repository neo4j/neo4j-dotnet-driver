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

using System.Buffers;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Xunit;

namespace Neo4j.Driver.Internal.Util;

public class PipeReaderMemoryPoolTests
{
    [Theory]
    [InlineData(512, 512, Constants.MaxReadBufferSize)]
    [InlineData(1024, 1024, Constants.MaxReadBufferSize)]
    [InlineData(1025, 2048, Constants.MaxReadBufferSize)]
    [InlineData(2049, 4096, Constants.MaxReadBufferSize)]
    [InlineData(4096, 4096, Constants.MaxReadBufferSize)]
    [InlineData(4097, 8192, Constants.MaxReadBufferSize)]
    [InlineData(65535, 65536, Constants.MaxReadBufferSize)]
    [InlineData(65539, 131072, Constants.MaxReadBufferSize)]
    [InlineData(1025, 1025, 1024)]
    public void ShouldRentMemoryInPower(int size, int expectedSize, int maxReadBufferSize)
    {
        var pool = new PipeReaderMemoryPool(1024, maxReadBufferSize);
        using var memoryOwner = pool.Rent(size);
        memoryOwner.Memory.Length.Should().Be(expectedSize);
    }

    [Fact]
    public void ShouldRentMemoryInPowerWithDefaultSize()
    {
        var pool = new PipeReaderMemoryPool(1024, Constants.MaxReadBufferSize);
        using var memoryOwner = pool.Rent();
        memoryOwner.Memory.Length.Should().Be(1024);
    }

    [Fact]
    public void ShouldReusePooledObjects()
    {
        var pool = new PipeReaderMemoryPool(1024, Constants.MaxReadBufferSize);

        int length;
        using (var memoryOwner = pool.Rent(4321))
        {
            length = memoryOwner.Memory.Length;
            memoryOwner.Memory.Span[0] = 1;
        }

        using (var memoryOwner = pool.Rent(4321))
        {
            memoryOwner.Memory.Length.Should().Be(length);
            memoryOwner.Memory.Span[0].Should().Be(1);
        }
    }

    /// <summary>
    /// This test is to verify the behaviour of the shared pool. It is an unnecessary test but it proves the behaviour
    /// to validate <see cref="ShouldNotReturnSharedPoolObjects"/>
    /// </summary>
    [Fact]
    public void SharedPoolShouldReturnSameValue()
    {
        var pool = MemoryPool<byte>.Shared;
        int length;
        using (var memoryOwner = pool.Rent(1024))
        {
            length = memoryOwner.Memory.Length;
            memoryOwner.Memory.Span[0] = 1;
        }

        using (var memoryOwner = pool.Rent(1024))
        {
            memoryOwner.Memory.Length.Should().Be(length);
            memoryOwner.Memory.Span[0].Should().Be(1);
        }
    }

    [Fact]
    public void ShouldNotReturnSharedPoolObjects()
    {
        var pool = MemoryPool<byte>.Shared;
        using (var memoryOwner = pool.Rent(1024))
        {
            memoryOwner.Memory.Length.Should().Be(1024);
            memoryOwner.Memory.Span[0] = 1;
        }

        pool = new PipeReaderMemoryPool(1024, 2048);
        using (var memoryOwner = pool.Rent(1024))
        {
            memoryOwner.Memory.Length.Should().Be(1024);
            memoryOwner.Memory.Span[0].Should().Be(0);
        }
    }

    [Fact]
    public void CanBorrowMaxLengthArray()
    {
        var pool = new PipeReaderMemoryPool(1024, Constants.MaxReadBufferSize);

        // 2146435071 is the max length of an array in .NET
        using (var memoryOwner = pool.Rent(2146435071))
        {
            // when returned to the pool, as it exceeds the max size of the pool, it will be discarded
            memoryOwner.Memory.Length.Should().Be(2146435071);
        }
    }

    [Fact]
    public void ShouldNotStoreLargerThanMaxReadBufferSize()
    {
        var pool = new PipeReaderMemoryPool(1024, 1024);

        using (var memory = pool.Rent(1025))
        {
            memory.Memory.Length.Should().Be(1025);
            memory.Memory.Span[0] = 1;
        }

        using (var memory = pool.Rent(1025))
        {
            memory.Memory.Length.Should().Be(1025);
            memory.Memory.Span[0].Should().Be(0);
        }
    }
}
