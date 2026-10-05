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
using System.IO.Pipelines;
using FluentAssertions;
using Neo4j.Driver.Bolt.Transport.Implementations;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.Transport;

[TestFixture]
internal class PipeReaderByteReaderReadExactlyTests
{
    [Test]
    public async Task ReadExactlyAsync_SingleRead_FillsBuffer()
    {
        var sequence = new ReadOnlySequence<byte>([1, 2, 3, 4]);
        var pipeReader = PipeReader.Create(sequence);
        var reader = new PipeReaderByteReader(pipeReader);

        var buffer = new byte[4];
        await reader.ReadExactlyAsync(buffer);

        buffer.Should().BeEquivalentTo([1, 2, 3, 4], o => o.WithStrictOrdering());
    }

    [Test]
    public async Task ReadExactlyAsync_MultipleReads_AccumulatesAndAdvancesCorrectly()
    {
        var pipe = new Pipe();
        var reader = new PipeReaderByteReader(pipe.Reader);

        _ = Task.Run(async () =>
        {
            await pipe.Writer.WriteAsync(new byte[] { 1, 2 }).ConfigureAwait(false);
            await pipe.Writer.FlushAsync().ConfigureAwait(false);
            await pipe.Writer.WriteAsync(new byte[] { 3, 4, 5 }).ConfigureAwait(false);
            await pipe.Writer.FlushAsync().ConfigureAwait(false);
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        });

        var buffer = new byte[5];
        await reader.ReadExactlyAsync(buffer);

        buffer.Should().BeEquivalentTo([1, 2, 3, 4, 5], o => o.WithStrictOrdering());
    }

    [Test]
    public void ReadExactlyAsync_EndOfStream_ThrowsEndOfStreamException()
    {
        var sequence = ReadOnlySequence<byte>.Empty;
        var pipeReader = PipeReader.Create(sequence);
        var reader = new PipeReaderByteReader(pipeReader);

        var act = async () => await reader.ReadExactlyAsync(new byte[1]);

        act.Should().ThrowAsync<EndOfStreamException>();
    }
}
