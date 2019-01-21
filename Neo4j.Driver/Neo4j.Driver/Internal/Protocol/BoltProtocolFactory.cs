// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Protocol
{
    internal static class BoltProtocolFactory
    {
        private static class ProtocolVersion
        {
            public const int NoVersion = 0;
            public const int Version1 = 1;
            public const int Version2 = 2;
            public const int Version3 = 3;
            public const int Http = 1213486160;
        }

        //This is a 'magic' handshake identifier to indicate we're using 'BOLT' ('GOGOBOLT')
        private const int BoltIdentifier = 0x6060B017;
        private static readonly int[] SupportedVersions = {ProtocolVersion.Version3, ProtocolVersion.Version2, ProtocolVersion.Version1, 0};

        public static IBoltProtocol ForVersion(int version)
        {
            switch (version)
            {
                case ProtocolVersion.Version1:
                    return BoltProtocolV1.BoltV1;
                case ProtocolVersion.Version2:
                    return BoltProtocolV2.BoltV2;
                case ProtocolVersion.Version3:
                    return BoltProtocolV3.BoltV3;
                case ProtocolVersion.NoVersion:
                    throw new NotSupportedException(
                        "The Neo4j server does not support any of the protocol versions supported by this client. " +
                        "Ensure that you are using driver and server versions that are compatible with one another.");
                case ProtocolVersion.Http:
                    throw new NotSupportedException(
                        "Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
                        $"(HTTP defaults to port 7474 whereas BOLT defaults to port {GraphDatabase.DefaultBoltPort})");
                default:
                    throw new NotSupportedException(
                        "Protocol error, server suggested unexpected protocol version: " + version);
            }
        }

        public static byte[] PackSupportedVersions()
        {
             return PackVersions(SupportedVersions);
        }

        public static int UnpackAgreedVersion(byte[] data)
        {
            return PackStreamBitConverter.ToInt32(data);
        }

        private static byte[] PackVersions(IEnumerable<int> versions)
        {
            var aLittleBitOfMagic = PackStreamBitConverter.GetBytes(BoltIdentifier);

            var bytes = new List<byte>(aLittleBitOfMagic);
            foreach (var version in versions)
            {
                bytes.AddRange(PackStreamBitConverter.GetBytes(version));
            }
            return bytes.ToArray();
        }
    }
}
