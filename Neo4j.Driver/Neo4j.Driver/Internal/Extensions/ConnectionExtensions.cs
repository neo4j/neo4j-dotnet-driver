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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal
{
    internal static class ConnectionExtensions
    {
        public static AccessMode GetEnforcedAccessMode(this IConnection connection)
        {
            return connection.Mode ??
                   throw new InvalidOperationException("Connection should have its Mode property set.");
        }

        public static bool SupportsMultidatabase(this IConnection connection)
        {
            var serverVersion = ServerVersion.From(connection.Server.Agent);
            var protocol = connection.BoltProtocol;
            return serverVersion >= new ServerVersion(4, 0, 0) &&
                   protocol.GetVersion() >= BoltProtocolV4_0.Version;
        }
    }
}