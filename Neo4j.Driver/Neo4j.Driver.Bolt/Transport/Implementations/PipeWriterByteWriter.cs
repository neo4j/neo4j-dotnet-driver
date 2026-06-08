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

using System.IO.Pipelines;
using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.Transport.Implementations;

/// <summary>
/// Adapts a <see cref="PipeWriter"/> to <see cref="IByteWriter"/> (e.g. paired with <see cref="PipeReaderByteReader"/>).
/// </summary>
public class PipeWriterByteWriter : IByteWriter
{
    private readonly PipeWriter _pipeWriter;

    public PipeWriterByteWriter(PipeWriter pipeWriter)
    {
        _pipeWriter = pipeWriter ?? throw new ArgumentNullException(nameof(pipeWriter));
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _pipeWriter.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        await _pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
