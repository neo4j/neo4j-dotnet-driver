// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResultConsume : ProtocolObject
{
    public ResultConsumeType data { get; set; } = new();
    [JsonIgnore] public IRecord Records { get; set; }
    [JsonIgnore] public IResultSummary Summary { get; set; }

    public override async Task ProcessAsync()
    {
        Summary = await ObjManager.GetObject<Result>(data.resultId).ConsumeAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Summary", new
        {
            query = GetQuery(Summary),
            queryType = GetQueryTypeAsStringCode(Summary),
            plan = GetPlan(Summary),
            notifications = CreateNotificationList(),
            database = Summary.Database?.Name,
            serverInfo = GetServerInfo(Summary),
            counters = GetCountersFromSummary(Summary),
            profile = MapToProfilePlan(Summary.Profile),
            resultAvailableAfter = GetTotalMilliseconds(Summary.ResultAvailableAfter),
            resultConsumedAfter = GetTotalMilliseconds(Summary.ResultConsumedAfter)
        }).Encode();
    }

    private static long? GetTotalMilliseconds(TimeSpan timespan)
    {
        return timespan.TotalMilliseconds >= 0L
            ? (long) timespan.TotalMilliseconds
            : default(long?);
    }

    private static object GetQuery(IResultSummary summary)
    {
        return summary?.Query == null
            ? null
            : new
            {
                text = summary.Query.Text,
                parameters = summary.Query.Parameters
                    .Select(x => new {x.Key, Value = NativeToCypher.Convert(x.Value)})
                    .ToDictionary(x => x.Key, x => x.Value)
            };
    }

    private static string GetQueryTypeAsStringCode(IResultSummary summary)
    {
        return summary?.QueryType switch
        {
            QueryType.ReadOnly => "r",
            QueryType.ReadWrite => "rw",
            QueryType.WriteOnly => "w",
            QueryType.SchemaWrite => "s",
            QueryType.Unknown => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static object GetPlan(IResultSummary summary)
    {
        return summary?.Plan == null
            ? null
            : MapToPlanJson(summary.Plan);
    }

    private static object GetServerInfo(IResultSummary summary)
    {
        return summary?.Server == null
            ? null
            : new
            {
                protocolVersion = summary.Server.ProtocolVersion,
                agent = summary.Server.Agent
            };
    }

    private static object GetCountersFromSummary(IResultSummary summary)
    {
        return new
        {
            constraintsAdded = summary.Counters.ConstraintsAdded,
            constraintsRemoved = summary.Counters.ConstraintsRemoved,
            nodesCreated = summary.Counters.NodesCreated,
            nodesDeleted = summary.Counters.NodesDeleted,
            relationshipsCreated = summary.Counters.RelationshipsCreated,
            relationshipsDeleted = summary.Counters.RelationshipsDeleted,
            propertiesSet = summary.Counters.PropertiesSet,
            labelsAdded = summary.Counters.LabelsAdded,
            labelsRemoved = summary.Counters.LabelsRemoved,
            indexesAdded = summary.Counters.IndexesAdded,
            indexesRemoved = summary.Counters.IndexesRemoved,
            systemUpdates = summary.Counters.SystemUpdates,
            containsUpdates = summary.Counters.ContainsUpdates,
            containsSystemUpdates = summary.Counters.ContainsSystemUpdates
        };
    }

    private static object MapToProfilePlan(IProfiledPlan plan)
    {
        if (plan == null)
            return null;

        if (plan.HasPageCacheStats)
            return new
            {
                args = plan.Arguments,
                operatorType = plan.OperatorType,
                children = plan.Children.Select(MapToProfilePlan).ToList(),
                identifiers = plan.Identifiers,
                time = plan.Time,
                pageCacheHitRatio = plan.PageCacheHitRatio,
                pageCacheMisses = plan.PageCacheMisses,
                pageCacheHits = plan.PageCacheHits,
                rows = plan.Records,
                dbHits = plan.DbHits
            };

        return new
        {
            args = plan.Arguments,
            operatorType = plan.OperatorType,
            children = plan.Children.Select(MapToProfilePlan).ToList(),
            identifiers = plan.Identifiers,
            rows = plan.Records,
            dbHits = plan.DbHits
        };
    }

    private static object MapToPlanJson(IPlan plan)
    {
        return new
        {
            args = plan.Arguments,
            operatorType = plan.OperatorType,
            children = plan.Children.Select(MapToPlanJson).ToList(),
            identifiers = plan.Identifiers
        };
    }

    private object CreateNotificationList()
    {
        if (Summary?.Notifications == null)
            return null;
        if (Summary?.Notifications?.All(x => x.Position == null) ?? false)
            return Summary?.Notifications.Select(x => new
            {
                severity = x.Severity,
                description = x.Description,
                code = x.Code,
                title = x.Title
            }).ToList();

        return Summary?.Notifications.Select(x => new
        {
            severity = x.Severity,
            description = x.Description,
            code = x.Code,
            title = x.Title,
            position = x.Position == null
                ? null
                : new
                {
                    column = x.Position.Column,
                    offset = x.Position.Offset,
                    line = x.Position.Line
                }
        }).ToList();
    }

    public class ResultConsumeType
    {
        public string resultId { get; set; }
    }
}