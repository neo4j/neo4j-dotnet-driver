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
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver;
using System.Linq;

namespace Neo4j.Driver.Internal.Protocol
{
    internal static class BoltProtocolFactory
    {
        //This is a 'magic' handshake identifier to indicate we're using 'BOLT' ('GOGOBOLT')
        private const int BoltIdentifier = 0x6060B017;
        private const int BoltHTTPIdentifier = 1213486160;  //0x‭48 54 54 50 - or HTTP ascii codes...

        private static readonly int[] SupportedVersions = { new BoltProtocolVersion(4, 4).PackToIntRange(new BoltProtocolVersion(4, 1)),
                                                            new BoltProtocolVersion(4, 1).PackToInt(),
                                                            new BoltProtocolVersion(4, 0).PackToInt(),
                                                            new BoltProtocolVersion(3, 0).PackToInt()};

        public static IBoltProtocol ForVersion(BoltProtocolVersion version, IDictionary<string, string> routingContext = null)
        {
            if (version.Equals(3, 0))
            {
                return new BoltProtocolV3();
            }
            else if (version.Equals(4, 0))
            {
                return new BoltProtocolV4_0();
            }
            else if (version.Equals(4, 1) )
            {
                return new BoltProtocolV4_1(routingContext);
            }
            else if (version.Equals(4, 2))
			{
                return new BoltProtocolV4_2(routingContext);
			}
            else if (version.Equals(4, 3))
            {
                return new BoltProtocolV4_3(routingContext);
            }
			else if (version.Equals(4, 4))
			{
				return new BoltProtocolV4_4(routingContext);
			}
			else if(version.Equals(0, 0))
			{
                throw new NotSupportedException(
                        "The Neo4j server does not support any of the protocol versions supported by this client. " +
                        "Ensure that you are using driver and server versions that are compatible with one another.");
            }
            else if (version == new BoltProtocolVersion(BoltHTTPIdentifier)) 
            {
                throw new NotSupportedException(
                    "Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
                    $"(HTTP defaults to port 7474 whereas BOLT defaults to port {GraphDatabase.DefaultBoltPort})");
            }
            else
            {
                throw new NotSupportedException(
                        "Protocol error, server suggested unexpected protocol version: " + version.MajorVersion + "." + version.MinorVersion);
            }
        }
        
        public static BoltProtocolVersion UnpackAgreedVersion(byte[] data)
        {            
            return BoltProtocolVersion.FromPackedInt(PackStreamBitConverter.ToInt32(data));
        }

        public static byte[] PackSupportedVersions(int numVersionsToPack)
        {   
            return PackVersions(SupportedVersions.Take(numVersionsToPack));
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