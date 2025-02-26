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
using System.Linq;
using System.Threading;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Protocol;

internal interface IBoltProtocolFactory
{
    IBoltProtocol ForVersion(BoltProtocolVersion version);
}

internal class BoltProtocolFactory : IBoltProtocolFactory
{
    private const int HTTPReservedMajorVersion = 80;
    private const int HTTPReservedMinorVersion = 84;
    public static readonly BoltProtocolVersion HttpReservedVersion = new(HTTPReservedMajorVersion, HTTPReservedMinorVersion);

    private const string HttpErrorMessage =
        "Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
        "(HTTP defaults to port 7474 whereas BOLT defaults to port 7687)";

    private const string NoAgreedVersion =
        "The Neo4j server does not support any of the protocol versions supported by this client. " +
        "Ensure that you are using driver and server versions that are compatible with one another.";

    internal static readonly BoltProtocolFactory Default = new();

    public static readonly BoltProtocolVersion[] SupportedVersions = new BoltProtocolVersion[]
    {
        BoltProtocolVersion.V3_0,
        BoltProtocolVersion.V4_0,
        BoltProtocolVersion.V4_1,
        BoltProtocolVersion.V4_2,
        BoltProtocolVersion.V4_3,
        BoltProtocolVersion.V4_4,
        BoltProtocolVersion.V5_0,
        BoltProtocolVersion.V5_1,
        BoltProtocolVersion.V5_2,
        BoltProtocolVersion.V5_3,
        BoltProtocolVersion.V5_4,
        BoltProtocolVersion.V5_5,
        BoltProtocolVersion.V5_6,
        BoltProtocolVersion.V5_7
    };

    private static readonly Lazy<byte[]> HandshakeBytesLazy =
        new(
            () =>
            {
                //This is a 'magic' handshake identifier to indicate we're using 'BOLT'
                //                    ('GO GO BOLT')
                const int goGoBolt = 0x_60_60_B017;

                var versions = new[]
                {
                    goGoBolt,
                    
                    //Announce support for the new handshake format with no manifest range supplied.
                    BoltProtocolVersion.HandshakeManifestV1.PackToInt(), 
                    
                    // 3 more versions max.
                    BoltProtocolVersion.V5_7.PackToIntRange(BoltProtocolVersion.V5_0),
                    BoltProtocolVersion.V4_4.PackToIntRange(BoltProtocolVersion.V4_2),
                    BoltProtocolVersion.V3_0.PackToInt()
                };

                return versions.SelectMany(PackStreamBitConverter.GetBytes).ToArray();
            },
            LazyThreadSafetyMode.PublicationOnly);

    private BoltProtocolFactory()
    {
    }

    public IBoltProtocol ForVersion(BoltProtocolVersion version)
    {
        if (version == HttpReservedVersion)
        {
            throw new NotSupportedException(HttpErrorMessage);
        }

        return version switch
        {
            // no matching versions
            { MajorVersion: 0, MinorVersion: 0 } => throw new NotSupportedException(NoAgreedVersion),
            { MajorVersion: 3, MinorVersion: 0 } => BoltProtocolV3.Instance,
            { MajorVersion: 4, MinorVersion: <= 4, MinorVersion: >= 1 } => BoltProtocol.Instance,
            { MajorVersion: 5, MinorVersion: <= 7, MinorVersion: >= 0 } => BoltProtocol.Instance,
            _ => throw new NotSupportedException(
                $"Protocol error, server suggested unexpected protocol version: {version}")
        };
    }
    
    public static (BoltProtocolVersion version, int range) UnpackAgreedVersion(byte[] data)
    {
        var packedInt = PackStreamBitConverter.ToInt32(data);
        return (BoltProtocolVersion.FromPackedInt(packedInt),
                BoltProtocolVersion.RangeFromPackedInt(packedInt));
    }

    public static byte[] PackSupportedVersions()
    {
        return HandshakeBytesLazy.Value;
    }
}
