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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal class RoutingTableCollector : IMetadataCollector<IDictionary<string, object>>
{
    internal const string RoutingTableKey = "rt";
    internal const string TimeoutKey = "ttl";
    internal const string ServersKey = "servers";
    internal const string DatabaseKey = "db";

    object IMetadataCollector.Collected => Collected;

    public IDictionary<string, object> Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata != null && metadata.TryGetValue(RoutingTableKey, out var routingTable))
        {
            if (routingTable is IDictionary<string, object> rt)
            {
                Collected = rt;
            }
            else
            {
                throw new ProtocolException(
                    $"Expected '{RoutingTableKey}' metadata to be of type 'Dictionary<string, object>', but got '{routingTable?.GetType().Name}'.");
            }
        }
    }
}
