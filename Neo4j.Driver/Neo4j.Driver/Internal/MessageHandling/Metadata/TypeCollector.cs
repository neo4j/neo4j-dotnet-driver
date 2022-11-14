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

internal class TypeCollector : IMetadataCollector<QueryType>
{
    internal const string TypeKey = "type";

    object IMetadataCollector.Collected => Collected;

    public QueryType Collected { get; private set; } = QueryType.Unknown;

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata != null && metadata.TryGetValue(TypeKey, out var typeValue))
        {
            if (typeValue is string type)
            {
                Collected = FromTypeCode(type);
            }
            else
            {
                throw new ProtocolException(
                    $"Expected '{TypeKey}' metadata to be of type 'String', but got '{typeValue?.GetType().Name}'.");
            }
        }
    }

    private static QueryType FromTypeCode(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "r":
                return QueryType.ReadOnly;

            case "rw":
                return QueryType.ReadWrite;

            case "w":
                return QueryType.WriteOnly;

            case "s":
                return QueryType.SchemaWrite;

            default:
                throw new ProtocolException($"An invalid value of '{type}' was passed as '{TypeKey}' metadata.");
        }
    }
}
