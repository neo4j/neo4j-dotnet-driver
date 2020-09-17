// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.V4_2
{
    internal class HelloResponseHandler : MetadataCollectingResponseHandler
    {
        private readonly IConnection _connection;
        private BoltProtocolVersion Version { get; set; }

        public HelloResponseHandler(IConnection connection, BoltProtocolVersion version)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Version = version ?? throw new ArgumentNullException("Attempting to create a HelloResponseHandler v4.1 with a null BoltProtocolVersion object");

            if (Version < new BoltProtocolVersion(4, 2))
                throw new ArgumentOutOfRangeException("Attempting to initialise a v4.1 HelloResponseHandler with a protocol version less than 4.2");

            AddMetadata<ServerVersionCollector, ServerVersion>();
            AddMetadata<ConnectionIdCollector, string>();
        }

        public override void OnSuccess(IDictionary<string, object> metadata)
        {
            base.OnSuccess(metadata);

            // From Server V4 extracting server from metadata in the success message is unreliable.
            // The server version is now tied to the protocol version.
            _connection.UpdateVersion(new ServerVersion(Version.MajorVersion, Version.MinorVersion, 0));

            _connection.UpdateId(GetMetadata<ConnectionIdCollector, string>());
        }
    }
}