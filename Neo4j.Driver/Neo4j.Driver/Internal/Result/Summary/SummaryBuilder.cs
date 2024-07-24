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

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.MessageHandling.Metadata;

namespace Neo4j.Driver.Internal.Result;

internal sealed class SummaryBuilder
{
    public SummaryBuilder(Query query, IServerInfo serverInfo)
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
        Server = serverInfo;
    }

    public Query Query { get; }
    public IServerInfo Server { get; }

    public QueryType QueryType { get; set; }
    public ICounters Counters { get; set; }
    public IPlan Plan { get; set; }
    public IProfiledPlan Profile { get; set; }
    public GqlStatusObjectsAndNotifications StatusAndNotifications { get; set; }
    public long ResultAvailableAfter { get; set; } = -1L;
    public long ResultConsumedAfter { get; set; } = -1L;
    public IDatabaseInfo Database { get; set; }

    public IResultSummary Build(CursorMetadata cursorMetadata)
    {
        return new ResultSummary(this, cursorMetadata);
    }

    public class ResultSummary : IResultSummary
    {
        public ResultSummary(SummaryBuilder builder, CursorMetadata cursorMetadata)
        {
            Query = builder.Query;
            QueryType = builder.QueryType;
            Counters = builder.Counters ?? new Counters();
            Profile = builder.Profile;
            Plan = Profile ?? builder.Plan;
            Notifications = builder.StatusAndNotifications?.FinalizeNotifications(cursorMetadata);
            GqlStatusObjects = builder.StatusAndNotifications?.FinalizeStatusObjects(cursorMetadata);
            ResultAvailableAfter = TimeSpan.FromMilliseconds(builder.ResultAvailableAfter);
            ResultConsumedAfter = TimeSpan.FromMilliseconds(builder.ResultConsumedAfter);
            Server = builder.Server;
            Database = builder.Database ?? new DatabaseInfo();
        }

        public Query Query { get; }
        public ICounters Counters { get; }
        public QueryType QueryType { get; }
        public bool HasPlan => Plan != null;
        public bool HasProfile => Profile != null;
        public IPlan Plan { get; }
        public IProfiledPlan Profile { get; }
        public IList<INotification> Notifications { get; }
        public IList<IGqlStatusObject> GqlStatusObjects { get; }
        public TimeSpan ResultAvailableAfter { get; }
        public TimeSpan ResultConsumedAfter { get; }
        public IServerInfo Server { get; }
        public IDatabaseInfo Database { get; }

        public override string ToString()
        {
            return $"{GetType().Name}{{{nameof(Query)}={Query}, " +
                $"{nameof(Counters)}={Counters}, " +
                $"{nameof(QueryType)}={QueryType}, " +
                $"{nameof(Plan)}={Plan}, " +
                $"{nameof(Profile)}={Profile}, " +
                $"{nameof(Notifications)}={Notifications.ToContentString()}, " +
                $"{nameof(ResultAvailableAfter)}={ResultAvailableAfter:g}, " +
                $"{nameof(ResultConsumedAfter)}={ResultConsumedAfter:g}, " +
                $"{nameof(Server)}={Server})," +
                $"{nameof(Database)}={Database}}}";
        }
    }
}
