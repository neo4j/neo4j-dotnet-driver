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

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    internal class QueryIdCollector : IMetadataCollector<long>
    {
        internal const string QueryIdKey = "qid";

        object IMetadataCollector.Collected => Collected;

        public long Collected { get; private set; } = -1;

        public void Collect(IDictionary<string, object> metadata)
        {
            if (metadata != null && metadata.TryGetValue(QueryIdKey, out var stmtIdValue))
            {
                if (stmtIdValue is long stmtId)
                {
                    Collected = stmtId;
                }
                else
                {
                    throw new ProtocolException(
                        $"Expected '{QueryIdKey}' metadata to be of type 'Int64', but got '{stmtIdValue?.GetType().Name}'.");
                }
            }
        }
    }
}