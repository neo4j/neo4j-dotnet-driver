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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;


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
    private List<BoltProtocolVersion> _protocolVersions = new List<BoltProtocolVersion>();
    public long CapabilitiesBitmask { get; private set; }
    public BoltProtocolVersion SelectedVersion { get; private set; }

    private BoltHandshaker()
    {
    }

    private static async Task<(BoltProtocolVersion version, BoltProtocolVersion range)> ParseProtocolVersionResponse(ITcpSocketClient socketClient, CancellationToken cancellationToken)
    {
        var responseBytes = new byte[4];

        //Read version supplied by the server
        var read = await socketClient.ReaderStream
            .ReadAsync(responseBytes, 0, responseBytes.Length, cancellationToken)
            .ConfigureAwait(false);

        if (read < responseBytes.Length)
        {
            throw new IOException(
                $"Unexpected end of stream when performing handshake, read only returned {read} bytes but expected {responseBytes.Length} bytes.");
        }

        var serverVersionResponse = BoltProtocolFactory.UnpackAgreedVersion(responseBytes);
        return (serverVersionResponse.version, serverVersionResponse.range) ;
    }

    private static bool IsManifestSytleHandshake(BoltProtocolVersion version)
    {
        return (version.MajorVersion == BoltProtocolVersion.ManifestSchema);
    }

    private static async Task<VarLong> ReadVariableLengthData(ITcpSocketClient socketClient, CancellationToken cancellationToken)
    {
        VarLong resultVariable = new VarLong();
        var responseByte = new byte[1];
        var moreData = true;

        while (moreData)
        {
            var bytesRead = await socketClient.ReaderStream
                .ReadAsync(responseByte, 0, responseByte.Length, cancellationToken)
                .ConfigureAwait(false);

            resultVariable.AddSegment(responseByte[0]);

            //If most significant bit of the byte is 1 then there are further bytes of the VarInt128 to follow
            moreData = (responseByte[0] >> 7) == 1;
        }

        return resultVariable;
    }

    private static async Task<long> ParseNumProtocolVersions(
        ITcpSocketClient socketClient,
        CancellationToken cancellationToken)
    {   
        var numProtocolVersions = await ReadVariableLengthData(socketClient, cancellationToken).ConfigureAwait(false);
        return numProtocolVersions.Value;
    }
    
    private async Task ParseSupportedProtocolVersions(
        ITcpSocketClient sockeClient, 
        CancellationToken cancellationToken)
    {
        var numProtocolVersions = await ParseNumProtocolVersions(sockeClient, cancellationToken)
            .ConfigureAwait(false);

        if (numProtocolVersions <= 0)
        {
            throw new ProtocolException("Server supplied a zero size list of acceptable protocols");
        }

        var responseBytes = new byte[4];       
        //Loop through the protocol versions reading each in and adding to supported list
        for (var i = 0; i < numProtocolVersions; i++)
        {
            var bytesRead = await sockeClient.ReaderStream
                .ReadAsync(responseBytes, 0, responseBytes.Length, cancellationToken)
                .ConfigureAwait(false);

            var protocolVersionAndRange = BoltProtocolFactory.UnpackAgreedVersion(responseBytes);
            
            foreach (var protocol in BoltProtocolFactory.SupportedVersions)
            {
                if (protocol >= protocolVersionAndRange.range && protocol <= protocolVersionAndRange.version)
                {
                    _protocolVersions.Add(protocol);
                }   
            }
            //TODO: debug this   
        }                                                                                     
    }

    private async Task ParseCapabilityBitmask(
        ITcpSocketClient socketClient,
        CancellationToken cancellationToken)
    {
        var bitmask = await ReadVariableLengthData(socketClient, cancellationToken).ConfigureAwait(false);
        CapabilitiesBitmask = bitmask.Value;
    }

    private void SelectProtocolVersion()
    {
        //We iterate through the versions supplied by the server in the manifest and select the newest one (highest version number)
        SelectedVersion = _protocolVersions.Max();    
    }

    private async Task EncodeAndSendHandshakeResponseAsync(
        ITcpSocketClient socketClient,
        CancellationToken cancellationToken)
    {
        var versionData = PackStreamBitConverter.GetBytes(SelectedVersion.PackToInt());
        var compatabilityData = new byte[] { 0x00 }; //TODO: method that will eventually return a bitmask built using VarInt

        var byteData = versionData.Concat(compatabilityData).ToArray(); //for performance can be changed to use block copy or similar

        await socketClient.WriterStream.WriteAsync(
                byteData,
                0,
                versionData.Length,
                cancellationToken)
            .ConfigureAwait(false);

        await socketClient.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void CheckManifestVersion(BoltProtocolVersion version, Uri connectionUri)
    {
        if (version.MinorVersion != BoltProtocolVersion.ManifestVersion)
        {
            throw new ProtocolException(
                $"Unsupported bolt protocol manifest version {version.MinorVersion} received from {connectionUri}");
        }
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

        //if the server has not responded indicating a manifest handshake is in effect
        //then it has responded with a protocol version that the driver should use. 
        var serverVersionResponse = await ParseProtocolVersionResponse(socketClient, cancellationToken).ConfigureAwait(false);
        if (!IsManifestSytleHandshake(serverVersionResponse.version))
        {
            return serverVersionResponse.version;
        }

        //We are now parsing a manifest style handshake...

        CheckManifestVersion(serverVersionResponse.version, socketClient.ConnectionUri);
        
        await ParseSupportedProtocolVersions(socketClient, cancellationToken).ConfigureAwait(false);

        await ParseCapabilityBitmask(socketClient, cancellationToken).ConfigureAwait(false);

        //TODO: logging for all this new handshake stuff.
        //logger.Debug("S: [HANDSHAKE] {0}.{1}", serverVersionResponse.MajorVersion, serverVersionResponse.MinorVersion);

        SelectProtocolVersion();

        await EncodeAndSendHandshakeResponseAsync(socketClient, cancellationToken);

        return SelectedVersion;
    }
}
