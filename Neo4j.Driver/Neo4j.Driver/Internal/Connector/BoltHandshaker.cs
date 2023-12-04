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

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Connector;

internal interface IBoltHandshaker
{
    Task<BoltProtocolVersion> DoHandshakeAsync(
        ITcpSocketClient socketClient,
        ILogger logger,
        CancellationToken cancellationToken);
}

internal sealed class BoltHandshaker : IBoltHandshaker
{
    internal static BoltHandshaker Default = new();

    private BoltHandshaker()
    {
    }

    public async Task<BoltProtocolVersion> DoHandshakeAsync(
        ITcpSocketClient socketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var data = BoltProtocolFactory.PackSupportedVersions();
        await socketClient.WriterStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        await socketClient.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        logger.Debug("C: [HANDSHAKE] {0}", data.ToHexString());

        var responseBytes = new byte[4];
        var read = await socketClient.ReaderStream
            .ReadAsync(responseBytes, 0, responseBytes.Length, cancellationToken)
            .ConfigureAwait(false);

        if (read < responseBytes.Length)
        {
            throw new IOException(
                $"Unexpected end of stream when performing handshake, read only returned {read} bytes but expected {responseBytes.Length} bytes.");
        }

        var agreedVersion = BoltProtocolFactory.UnpackAgreedVersion(responseBytes);

        logger.Debug("S: [HANDSHAKE] {0}.{1}", agreedVersion.MajorVersion, agreedVersion.MinorVersion);

        return agreedVersion;
    }
}
