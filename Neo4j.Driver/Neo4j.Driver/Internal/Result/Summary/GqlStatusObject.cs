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
using System.Collections.ObjectModel;

namespace Neo4j.Driver.Internal.Result;

internal sealed record GqlStatusObject(
    string GqlStatus,
    string StatusDescription,
    IInputPosition Position,
    string RawClassification,
    string RawSeverity,
    IReadOnlyDictionary<string, object> DiagnosticRecord,
    string Title,
    bool IsNotification)
    : IGqlStatusObject
{
    private GqlStatusObject(string gqlStatus, string description): this(
        gqlStatus,
        description,
        null,
        null,
        null,
        new ReadOnlyDictionary<string, object>(
            new Dictionary<string, object>
            {
                ["OPERATION"] = string.Empty,
                ["OPERATION_CODE"] = "0",
                ["CURRENT_SCHEMA"] = "/"
            }),
        null,
        false)
    {
    }

    public string GqlStatus { get; } = GqlStatus ?? throw new ArgumentNullException(nameof(GqlStatus));

    public string StatusDescription { get; } =
        StatusDescription ?? throw new ArgumentNullException(nameof(StatusDescription));

    public NotificationClassification NotificationClassification => ClassificationFrom(RawClassification);

    private NotificationClassification ClassificationFrom(string rawClassification)
    {
        return rawClassification?.ToLowerInvariant() switch
        {
            "hint" => NotificationClassification.Hint,
            "unrecognized" => NotificationClassification.Unrecognized,
            "unsupported" => NotificationClassification.Unsupported,
            "performance" => NotificationClassification.Performance,
            "deprecation" => NotificationClassification.Deprecation,
            "security" => NotificationClassification.Security,
            "topology" => NotificationClassification.Topology,
            "generic" => NotificationClassification.Generic,
            _ => NotificationClassification.Unknown
        };
    }

    public NotificationSeverity Severity => Notification.ParseSeverity(RawSeverity);

    public IReadOnlyDictionary<string, object> DiagnosticRecord { get; } =
        DiagnosticRecord ?? throw new ArgumentNullException(nameof(DiagnosticRecord));

    internal static readonly IGqlStatusObject OmittedResult = new GqlStatusObject(
        "00001",
        "note: successful completion - omitted result");

    internal static IGqlStatusObject Success = new GqlStatusObject(
        "00000",
        "note: successful completion");


    internal static readonly IGqlStatusObject NoData = new GqlStatusObject(
        "02000",
        "note: no data"
     );

    internal static readonly IGqlStatusObject NoDataUnknown = new GqlStatusObject(
        "02N42",
        "note: no data - unknown subcondition"
      );
}
