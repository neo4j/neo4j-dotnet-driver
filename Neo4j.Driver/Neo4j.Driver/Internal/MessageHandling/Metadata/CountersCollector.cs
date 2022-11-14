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
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal class CountersCollector : IMetadataCollector<ICounters>
{
    internal const string CountersKey = "stats";

    object IMetadataCollector.Collected => Collected;

    public ICounters Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata == null || !metadata.TryGetValue(CountersKey, out var countersValue))
        {
            return;
        }

        switch (countersValue)
        {
            case null:
                Collected = null;
                break;

            case IDictionary<string, object> countersDict:
                Collected = new Counters(
                    CountersValue(countersDict, "nodes-created"),
                    CountersValue(countersDict, "nodes-deleted"),
                    CountersValue(countersDict, "relationships-created"),
                    CountersValue(countersDict, "relationships-deleted"),
                    CountersValue(countersDict, "properties-set"),
                    CountersValue(countersDict, "labels-added"),
                    CountersValue(countersDict, "labels-removed"),
                    CountersValue(countersDict, "indexes-added"),
                    CountersValue(countersDict, "indexes-removed"),
                    CountersValue(countersDict, "constraints-added"),
                    CountersValue(countersDict, "constraints-removed"),
                    CountersValue(countersDict, "system-updates"),
                    countersDict.GetValue<bool?>("contains-system-updates", null),
                    countersDict.GetValue<bool?>("contains-updates", null));

                break;

            default:
                throw new ProtocolException(
                    $"Expected '{CountersKey}' metadata to be of type 'IDictionary<String,Object>', but got '{countersValue?.GetType().Name}'.");
        }
    }

    private static int CountersValue(IDictionary<string, object> counters, string name)
    {
        return (int)counters.GetValue(name, 0L);
    }
}
