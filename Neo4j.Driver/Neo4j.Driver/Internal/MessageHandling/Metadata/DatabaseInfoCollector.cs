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
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal sealed class DatabaseInfoCollector : IMetadataCollector<IDatabaseInfo>
{
    internal const string DbKey = "db";
    object IMetadataCollector.Collected => Collected;

    public IDatabaseInfo Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        var databaseName = (string)null;

        if (metadata != null && metadata.TryGetValue(DbKey, out var dbValue))
        {
            if (dbValue is string db)
            {
                databaseName = db;
            }
            else
            {
                throw new ProtocolException(
                    $"Expected '{DbKey}' metadata to be of type 'string', but got '{dbValue?.GetType().Name}'.");
            }
        }

        Collected = new DatabaseInfo(databaseName);
    }
}
