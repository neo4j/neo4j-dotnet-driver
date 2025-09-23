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
    
    private BoltHandshaker()
    {
    }

    private static async Task<(BoltProtocolVersion version, int range)> ParseProtocolVersionResponse(ITcpSocketClient socketClient, ILogger logger, CancellationToken cancellationToken)
    {
        var responseBytes = new byte[4];

        //Read version supplied by the server
        var read = await socketClient.ReaderStream
            .ReadAsync(responseBytes, 0, responseBytes.Length, cancellationToken)
            .ConfigureAwait(false);

        logger.Debug("S: [HANDSHAKE] Suggested protocol version - {0}", responseBytes.ToHexString());

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

    private static async Task<VarLong> ReadVariableLengthData(ITcpSocketClient socketClient, ILogger logger, CancellationToken cancellationToken)
    {
        VarLong resultVariable = new VarLong();
        var responseByte = new byte[1];
        var moreData = true;
        var serverResponse = String.Empty; 

        while (moreData)
        {
            var bytesRead = await socketClient.ReaderStream
                .ReadAsync(responseByte, 0, responseByte.Length, cancellationToken)
                .ConfigureAwait(false);

            resultVariable.AddSegment(responseByte[0]);

            //If most significant bit of the byte is 1 then there are further bytes of the VarInt128 to follow
            moreData = (responseByte[0] >> 7) == 1;

            serverResponse += responseByte.ToHexString() + " ";
        }

        logger.Debug("S: [HANDSHAKE] VarInt -  {0}", serverResponse);
        return resultVariable;
    }

    private static async Task<long> ParseNumProtocolVersions(
        ITcpSocketClient socketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {   
        var numProtocolVersions = await ReadVariableLengthData(socketClient, logger, cancellationToken).ConfigureAwait(false);
        return numProtocolVersions.Value;
    }
    
    private static async Task<List<BoltProtocolVersion>> ParseSupportedProtocolVersions(
        ITcpSocketClient sockeClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var numProtocolVersions = await ParseNumProtocolVersions(sockeClient, logger, cancellationToken)
            .ConfigureAwait(false);

        if (numProtocolVersions <= 0)
        {
            throw new ProtocolException("Server supplied a zero size list of acceptable protocols");
        }


        var protocolVersions = new List<BoltProtocolVersion>();

        var responseBytes = new byte[4];       
        //Loop through the protocol versions reading each in and adding to supported list
        for (var i = 0; i < numProtocolVersions; i++)
        {
            var bytesRead = await sockeClient.ReaderStream
                .ReadAsync(responseBytes, 0, responseBytes.Length, cancellationToken)
                .ConfigureAwait(false);

            logger.Debug("S: [HANDSHAKE] Supported protocol version and range - {0}", responseBytes.ToHexString());

            var protocolVersionAndRange = BoltProtocolFactory.UnpackAgreedVersion(responseBytes);
            var lowestVersion = new BoltProtocolVersion(protocolVersionAndRange.version.MajorVersion,
                                                         protocolVersionAndRange.version.MinorVersion - protocolVersionAndRange.range);
            
            //If the protocol version from the server is one that this driver knows about add it to the list.
            foreach (var protocol in BoltProtocolFactory.SupportedVersions)
            {
                if (protocol >= lowestVersion && protocol <= protocolVersionAndRange.version)
                {
                    protocolVersions.Add(protocol);
                }   
            }  
        }

        return protocolVersions;
    }

    private static async Task<long> ParseCapabilityBitmask(
        ITcpSocketClient socketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var bitmask = await ReadVariableLengthData(socketClient, logger, cancellationToken).ConfigureAwait(false);
        return bitmask.Value;
    }

    private static BoltProtocolVersion SelectProtocolVersion(List<BoltProtocolVersion> protocolVersions)
    {
        //We now select the highest version that is in the list. This list contains the set of versions that both
        //the server and the driver know about. 
        return protocolVersions.Max();
    }

    private static async Task EncodeAndSendHandshakeResponseAsync(
        ITcpSocketClient socketClient,
        BoltProtocolVersion selectedVersion,
        long capabilitiesBitMask,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var versionData = PackStreamBitConverter.GetBytes(selectedVersion.PackToInt());
        var compatabilityData = new byte[] { 0x00 }; //TODO: method that will eventually return a bitmask built using VarInt

        var byteData = versionData.Concat(compatabilityData).ToArray(); //for performance can be changed to use block copy or similar

        await socketClient.WriterStream.WriteAsync(
                byteData,
                0,
                byteData.Length,
                cancellationToken)
            .ConfigureAwait(false);

        await socketClient.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        logger.Debug("C: [HANDSHAKE] Selected version and capabilities {0}", byteData.ToHexString());
    }

    private static void CheckManifestVersion(BoltProtocolVersion version, Uri connectionUri)
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

        logger.Debug("C: [HANDSHAKE] Driver supported versions - {0}", data.ToHexString());

        //if the server has not responded indicating a manifest handshake is in effect
        //then it has responded with a protocol version that the driver should use. 
        var serverVersionResponse = await ParseProtocolVersionResponse(socketClient, logger, cancellationToken).ConfigureAwait(false);
        if (!IsManifestSytleHandshake(serverVersionResponse.version))
        {
            return serverVersionResponse.version;
        }

        //We are now parsing a manifest style handshake...

        CheckManifestVersion(serverVersionResponse.version, socketClient.ConnectionUri);
        
        var protocolVersions = await ParseSupportedProtocolVersions(socketClient, logger, cancellationToken).ConfigureAwait(false);

        var capabilitiesBitMask = await ParseCapabilityBitmask(socketClient, logger, cancellationToken).ConfigureAwait(false);
        
        var selectedVersion = SelectProtocolVersion(protocolVersions);

        await EncodeAndSendHandshakeResponseAsync(socketClient, selectedVersion, capabilitiesBitMask, logger, cancellationToken);

        return selectedVersion;
    }
}
