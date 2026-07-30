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

#pragma warning disable CS0618 // Notifications is obsolete but still part of the wire contract.

using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;

namespace Neo4j.Driver.TestKitBackend.Summary;

internal interface ISummaryMapper
{
    SummaryResponse Map(IResultSummary summary);
}

internal class SummaryMapper : ISummaryMapper
{
    private readonly INativeToCypherMapper _cypherMapper;

    public SummaryMapper(INativeToCypherMapper cypherMapper)
    {
        _cypherMapper = cypherMapper;
    }

    public SummaryResponse Map(IResultSummary summary)
    {
        return new SummaryResponse(
            MapQuery(summary.Query),
            MapQueryType(summary.QueryType),
            summary.HasPlan ? MapPlan(summary.Plan) : null,
            summary.HasProfile ? MapProfile(summary.QueryProfile) : null,
            summary.Notifications?.Select(MapNotification).ToList() ?? [],
            summary.Database?.Name,
            MapServerInfo(summary.Server),
            MapCounters(summary.Counters),
            MapMilliseconds(summary.ResultAvailableAfter),
            MapMilliseconds(summary.ResultConsumedAfter),
            // IGqlStatusObject doesn't expose IsNotification - only the internal concrete type does.
            summary.GqlStatusObjects?.OfType<GqlStatusObject>().Select(MapGqlStatusObject).ToList() ?? []);
    }

    private SummaryQueryResponse MapQuery(Query query)
    {
        return new SummaryQueryResponse(
            query.Text,
            query.Parameters.ToDictionary(kv => kv.Key, kv => _cypherMapper.Map(kv.Value)));
    }

    private static string? MapQueryType(QueryType queryType)
    {
        return queryType switch
        {
            QueryType.ReadOnly => "r",
            QueryType.ReadWrite => "rw",
            QueryType.WriteOnly => "w",
            QueryType.SchemaWrite => "s",
            QueryType.Unknown => null,
            _ => throw new ArgumentOutOfRangeException(nameof(queryType))
        };
    }

    private static SummaryServerInfoResponse MapServerInfo(IServerInfo server)
    {
        return new SummaryServerInfoResponse(server.Address, server.Agent, server.ProtocolVersion);
    }

    private static SummaryCountersResponse MapCounters(ICounters counters)
    {
        return new SummaryCountersResponse(
            counters.ContainsUpdates,
            counters.NodesCreated,
            counters.NodesDeleted,
            counters.RelationshipsCreated,
            counters.RelationshipsDeleted,
            counters.PropertiesSet,
            counters.LabelsAdded,
            counters.LabelsRemoved,
            counters.IndexesAdded,
            counters.IndexesRemoved,
            counters.ConstraintsAdded,
            counters.ConstraintsRemoved,
            counters.SystemUpdates,
            counters.ContainsSystemUpdates);
    }

    private static long? MapMilliseconds(TimeSpan timeSpan)
    {
        return timeSpan.TotalMilliseconds >= 0 ? (long)timeSpan.TotalMilliseconds : null;
    }

    private static SummaryPositionResponse? MapPosition(IInputPosition position)
    {
        return position is null ? null : new SummaryPositionResponse(position.Column, position.Offset, position.Line);
    }

    private static SummaryNotificationResponse MapNotification(INotification notification)
    {
        return new SummaryNotificationResponse(
            notification.RawCategory ?? "",
            notification.Category.ToString().ToUpperInvariant(),
            notification.RawSeverityLevel ?? "",
            notification.SeverityLevel.ToString().ToUpperInvariant(),
            notification.Description,
            notification.Code,
            notification.Title,
            MapPosition(notification.Position));
    }

    private SummaryPlanResponse MapPlan(IPlan plan)
    {
        return new SummaryPlanResponse(
            plan.Arguments,
            plan.OperatorType,
            plan.Children.Select(MapPlan).ToList(),
            plan.Identifiers.ToList());
    }

    private SummaryProfileResponse MapProfile(IQueryProfile profile)
    {
        return new SummaryProfileResponse(
            profile.Arguments,
            profile.OperatorType,
            profile.Children.Select(MapProfile).ToList(),
            profile.Identifiers.ToList(),
            profile.Time,
            profile.PageCacheHitRatio,
            profile.PageCacheMisses,
            profile.PageCacheHits,
            profile.Rows,
            profile.DbHits);
    }

    private SummaryGqlStatusObjectResponse MapGqlStatusObject(GqlStatusObject gqlStatusObject)
    {
        return new SummaryGqlStatusObjectResponse(
            gqlStatusObject.GqlStatus,
            gqlStatusObject.StatusDescription,
            gqlStatusObject.DiagnosticRecord.ToDictionary(kv => kv.Key, kv => _cypherMapper.Map(kv.Value)),
            gqlStatusObject.Classification.ToString().ToUpperInvariant(),
            gqlStatusObject.RawClassification,
            gqlStatusObject.RawSeverity,
            gqlStatusObject.Severity.ToString().ToUpperInvariant(),
            MapPosition(gqlStatusObject.Position),
            gqlStatusObject.IsNotification);
    }
}
