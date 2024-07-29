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
using System.Linq;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CS0618 // Type or member is obsolete - but still needs to be handled

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Result;

internal static class SummaryJsonSerializer
{
    public static JRaw SerializeToRaw(IResultSummary summary)
    {
        return new JRaw(
            JsonConvert.SerializeObject(
                new
                {
                    query = GetQuery(summary),
                    queryType = GetQueryTypeAsStringCode(summary),
                    plan = MapToPlanJson(summary?.Plan),
                    notifications = MapNotifications(summary?.Notifications),
                    database = summary.Database?.Name,
                    serverInfo = GetServerInfo(summary),
                    counters = GetCountersFromSummary(summary.Counters),
                    profile = MapToProfilePlan(summary.Profile),
                    resultAvailableAfter = GetTotalMilliseconds(summary.ResultAvailableAfter),
                    resultConsumedAfter = GetTotalMilliseconds(summary.ResultConsumedAfter),
                    gqlStatusObjects = MapGqlStatusObjects(
                        summary.GqlStatusObjects)
                }));
    }

    private static long? GetTotalMilliseconds(TimeSpan timespan)
    {
        return timespan.TotalMilliseconds >= 0L
            ? (long)timespan.TotalMilliseconds
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
                    .Select(x => new { x.Key, Value = NativeToCypher.Convert(x.Value) })
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

    private static object GetCountersFromSummary(ICounters counters)
    {
        return new
        {
            constraintsAdded = counters.ConstraintsAdded,
            constraintsRemoved = counters.ConstraintsRemoved,
            nodesCreated = counters.NodesCreated,
            nodesDeleted = counters.NodesDeleted,
            relationshipsCreated = counters.RelationshipsCreated,
            relationshipsDeleted = counters.RelationshipsDeleted,
            propertiesSet = counters.PropertiesSet,
            labelsAdded = counters.LabelsAdded,
            labelsRemoved = counters.LabelsRemoved,
            indexesAdded = counters.IndexesAdded,
            indexesRemoved = counters.IndexesRemoved,
            systemUpdates = counters.SystemUpdates,
            containsUpdates = counters.ContainsUpdates,
            containsSystemUpdates = counters.ContainsSystemUpdates
        };
    }

    private static object MapToProfilePlan(IProfiledPlan plan)
    {
        if (plan == null)
        {
            return null;
        }

        if (plan.HasPageCacheStats)
        {
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
        }

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
        if (plan == null)
        {
            return null;
        }

        return new
        {
            args = plan.Arguments,
            operatorType = plan.OperatorType,
            children = plan.Children.Select(MapToPlanJson).ToList(),
            identifiers = plan.Identifiers
        };
    }

    private static object MapNotifications(IList<INotification> notifications)
    {
        if (notifications == null)
        {
            return null;
        }

        if (notifications.All(x => x.Position == null))
        {
            return notifications.Select(
                    x => new
                    {
                        rawCategory = x.RawCategory ?? string.Empty,
                        category = x.Category.ToString().ToUpper(),
                        severity = x.Severity,
                        rawSeverityLevel = x.RawSeverityLevel ?? string.Empty,
                        severityLevel = x.SeverityLevel.ToString().ToUpper(),
                        description = x.Description,
                        code = x.Code,
                        title = x.Title
                    })
                .ToList();
        }

        return notifications.Select(
                x => new
                {
                    rawCategory = x.RawCategory ?? string.Empty,
                    category = x.Category.ToString().ToUpper(),
                    severity = x.Severity,
                    rawSeverityLevel = x.RawSeverityLevel ?? string.Empty,
                    severityLevel = x.SeverityLevel.ToString().ToUpper(),
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
                })
            .ToList();
    }

    private static object MapGqlStatusObjects(IList<IGqlStatusObject> statusObjects)
    {
        if (statusObjects == null)
        {
            return Array.Empty<object>();
        }

        return statusObjects
            .OfType<GqlStatusObject>()
            .Select(
                x => new Dictionary<string, object>
                {
                    ["gqlStatus"] = x.GqlStatus,
                    ["statusDescription"] = x.StatusDescription,
                    ["diagnosticRecord"] = x.DiagnosticRecord.ToDictionary(y => y.Key, y => NativeToCypher.Convert(y.Value)),
                    ["classification"] = x.Classification.ToString().ToUpper(),
                    ["rawClassification"] = x.RawClassification,
                    ["rawSeverity"] = x.RawSeverity,
                    ["severity"] = x.Severity.ToString().ToUpper(),
                    ["position"] = x.Position == null
                        ? null
                        : new
                        {
                            column = x.Position.Column,
                            offset = x.Position.Offset,
                            line = x.Position.Line
                        },
                    ["isNotification"] = x.IsNotification
                });
    }
}
