// Copyright (c) "Neo4j"
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
using System.Linq;
using System.Threading;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Protocol
{
    internal static class BoltProtocolFactory
    {
        private const int BoltHttpIdentifier = 1213486160;  // 0x‭48 54 54 50 - or HTTP ascii codes...
        static readonly string HttpErrorMessage = "Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
                                               $"(HTTP defaults to port 7474 whereas BOLT defaults to port {GraphDatabase.DefaultBoltPort})";
        private static readonly string NoAgreedVersion =
            "The Neo4j server does not support any of the protocol versions supported by this client. " +
            "Ensure that you are using driver and server versions that are compatible with one another.";

        private static readonly Lazy<byte[]> HandshakeBytesLazy = 
            new(() =>
            {
                const int goGoBolt = 0x6060B017; 

                var versions = new int[]
                {
                    //This is a 'magic' handshake identifier to indicate we're using 'BOLT' ('GOGOBOLT')
                    goGoBolt,
                    // 4 versions max.
                    BoltProtocolVersion.V5_0.PackToInt(),
                    BoltProtocolVersion.V4_4.PackToIntRange(BoltProtocolVersion.V4_2),
                    BoltProtocolVersion.V4_1.PackToInt(),
                    BoltProtocolVersion.V30.PackToInt()
                };
                return versions.SelectMany(PackStreamBitConverter.GetBytes).ToArray();
            }, LazyThreadSafetyMode.PublicationOnly);

        public static IBoltProtocol ForVersion(BoltProtocolVersion version)
        {
            return version switch
            {
                {MajorVersion: 3, MinorVersion: 0} => new BoltProtocolV3(),
                {MajorVersion: 4, MinorVersion: 1} => new BoltProtocolV4_0(),
                {MajorVersion: 4, MinorVersion: 2} => new BoltProtocolV4_0(),
                {MajorVersion: 4, MinorVersion: 3} => new BoltProtocolV4_3(),
                {MajorVersion: 4, MinorVersion: 4} => new BoltProtocolV4_4(),
                {MajorVersion: 5, MinorVersion: 0} => new BoltProtocolV4_4(),
                // no matching versions
                {MajorVersion: 0, MinorVersion: 0} => throw new NotSupportedException(NoAgreedVersion),
                // http response
                _ when version == new BoltProtocolVersion(BoltHttpIdentifier) => throw new NotSupportedException(HttpErrorMessage),
                // undefined
                _ => throw new NotSupportedException($"Protocol error, server suggested unexpected protocol version: {version}")
            };
        }
        
        public static BoltProtocolVersion UnpackAgreedVersion(byte[] data)
        {            
            return BoltProtocolVersion.FromPackedInt(PackStreamBitConverter.ToInt32(data));
        }

        public static byte[] PackSupportedVersions() => HandshakeBytesLazy.Value;
    }
}