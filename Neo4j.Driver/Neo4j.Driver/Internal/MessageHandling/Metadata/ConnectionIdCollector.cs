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

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal class ConnectionIdCollector : IMetadataCollector<string>
{
    internal const string ConnectionIdKey = "connection_id";

    object IMetadataCollector.Collected => Collected;

    public string Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata != null && metadata.TryGetValue(ConnectionIdKey, out var connectionIdValue))
        {
            if (connectionIdValue is string connectionId)
            {
                Collected = connectionId;
            }
            else
            {
                throw new ProtocolException(
                    $"Expected '{ConnectionIdKey}' metadata to be of type 'String', but got '{connectionIdValue?.GetType().Name}'.");
            }
        }
    }
}
