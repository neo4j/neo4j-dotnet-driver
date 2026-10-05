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
using Neo4j.Driver.Bolt.Transport.Abstractions;
using Neo4j.Driver.Bolt.Transport.Implementations;

namespace Neo4j.Driver.Bolt.Tests.Transport;

/// <summary>
/// Builds <see cref="IByteReader"/> instances for tests that exercise chunk / stream decoding.
/// </summary>
internal static class TestByteReaders
{
    /// <summary>
    /// Returns a reader that exposes all of <paramref name="wireBytes"/> in one completed read
    /// (typical for in-memory Bolt message framing tests).
    /// </summary>
    public static IByteReader FromSingleReadBuffer(byte[] wireBytes)
    {
        ArgumentNullException.ThrowIfNull(wireBytes);
        var sequence = new ReadOnlySequence<byte>(wireBytes);
        var pipeReader = PipeReader.Create(sequence);
        return new PipeReaderByteReader(pipeReader);
    }
}
