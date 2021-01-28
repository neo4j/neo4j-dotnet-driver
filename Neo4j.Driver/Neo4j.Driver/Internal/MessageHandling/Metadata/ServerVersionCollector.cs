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

using System.Collections.Generic;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    internal class ServerVersionCollector : IMetadataCollector<ServerVersion>
    {
        internal const string ServerKey = "server";

        object IMetadataCollector.Collected => Collected;

        public ServerVersion Collected { get; private set; }

        public void Collect(IDictionary<string, object> metadata)
        {
            if (metadata != null && metadata.TryGetValue(ServerKey, out var serverValue))
            {
                if (serverValue is string server)
                {
                    Collected = ServerVersion.From(server);
                }
                else
                {
                    throw new ProtocolException(
                        $"Expected '{ServerKey}' metadata to be of type 'String', but got '{serverValue?.GetType().Name}'.");
                }
            }
        }
    }
}