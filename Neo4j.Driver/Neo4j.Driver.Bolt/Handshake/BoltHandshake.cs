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

using System.Buffers.Binary;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.Extensions;
using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.Handshake;

/// <summary>
/// Default Bolt socket handshake orchestration. Legacy completion path is implemented; manifest follow-up
/// (see <c>BoltHandshaker</c> in Neo4j.Driver) remains TODO here.
/// </summary>
internal class BoltHandshake : IBoltHandshake
{
    private const string NoAgreedVersion =
        "The Neo4j server does not support any of the protocol versions supported by this client. " +
        "Ensure that you are using driver and server versions that are compatible with one another.";

    private const string HttpEndpointMessage =
        "Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
        "(HTTP defaults to port 7474 whereas BOLT defaults to port 7687)";

    /// <inheritdoc />
    public async ValueTask<BoltHandshakeVersion> NegotiateAsync(
        IByteWriter writer,
        IByteReader reader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(reader);

        await writer.WriteAsync(BoltHandshakeClientOffers.Default, cancellationToken).ConfigureAwait(false);

        var responseWord = new byte[sizeof(int)];
        await reader.ReadExactlyAsync(responseWord, cancellationToken).ConfigureAwait(false);
        var version = BoltHandshakeVersion.FromBytes(responseWord);

        NotImplementedException.ThrowIf(version.IsManifestMarker); // TODO: implement manifest follow-up
        ProtocolException.ThrowIf(version is {Major: 0, Minor: 0}, NoAgreedVersion);
        NotSupportedException.ThrowIf(version.IsHttpResponse, HttpEndpointMessage);

        return version;
    }
}
