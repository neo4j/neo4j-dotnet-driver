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

using System.Text.Json.Serialization;
using Neo4j.Driver.TestKitBackend.Cypher;

namespace Neo4j.Driver.TestKitBackend.Summary;

internal record SummaryQueryResponse(string? Text, IReadOnlyDictionary<string, ICypherValue> Parameters);

internal record SummaryCountersResponse(
    bool ContainsUpdates,
    int NodesCreated,
    int NodesDeleted,
    int RelationshipsCreated,
    int RelationshipsDeleted,
    int PropertiesSet,
    int LabelsAdded,
    int LabelsRemoved,
    int IndexesAdded,
    int IndexesRemoved,
    int ConstraintsAdded,
    int ConstraintsRemoved,
    int SystemUpdates,
    bool ContainsSystemUpdates);

internal record SummaryServerInfoResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required string? Address { get; init; }
    public required string? Agent { get; init; }
    public required string? ProtocolVersion { get; init; }
}

internal record SummaryPositionResponse(int Column, int Offset, int Line);

internal record SummaryNotificationResponse
{
    public required string RawCategory { get; init; }
    public required string Category { get; init; }
    public required string RawSeverityLevel { get; init; }
    public required string SeverityLevel { get; init; }
    public required string? Description { get; init; }
    public required string? Code { get; init; }
    public required string? Title { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required SummaryPositionResponse? Position { get; init; }
}

internal record SummaryPlanResponse(
    IDictionary<string, object> Args,
    string OperatorType,
    IReadOnlyList<SummaryPlanResponse> Children,
    IReadOnlyList<string> Identifiers);

internal record SummaryProfileResponse(
    IDictionary<string, object> Args,
    string OperatorType,
    IReadOnlyList<SummaryProfileResponse> Children,
    IReadOnlyList<string> Identifiers,
    long? Time,
    double? PageCacheHitRatio,
    long? PageCacheMisses,
    long? PageCacheHits,
    long? Rows,
    long? DbHits);

internal record SummaryGqlStatusObjectResponse(
    string GqlStatus,
    string StatusDescription,
    IReadOnlyDictionary<string, ICypherValue> DiagnosticRecord,
    string Classification,
    string? RawClassification,
    string? RawSeverity,
    string Severity,
    SummaryPositionResponse? Position,
    bool IsNotification);
