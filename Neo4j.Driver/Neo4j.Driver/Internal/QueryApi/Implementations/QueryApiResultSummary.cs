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

#nullable enable

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Result;

using Neo4j.Driver.Internal.DependencyInjection;
namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiResultSummary : IResultSummary
{
    public QueryApiResultSummary(Query query, IServerInfo serverInfo, string database)
    {
        Query = query;
        Server = serverInfo;
        Database = new DatabaseInfo(database);
        Counters = new Counters();
    }

    public Query Query { get; }
    public ICounters Counters { get; }
    public QueryType QueryType => QueryType.Unknown;
    public bool HasPlan => false;
    public bool HasProfile => false;
    public IPlan? Plan => null;
    public IProfiledPlan? Profile => null;

#pragma warning disable CS0618
    public IList<INotification>? Notifications => null;
#pragma warning restore CS0618

    public IList<IGqlStatusObject>? GqlStatusObjects => null;
    public TimeSpan ResultAvailableAfter => TimeSpan.FromMilliseconds(-1);
    public TimeSpan ResultConsumedAfter => TimeSpan.FromMilliseconds(-1);
    public IServerInfo Server { get; }
    public IDatabaseInfo Database { get; }
}
