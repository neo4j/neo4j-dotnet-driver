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

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal sealed class GqlStatusObjectsAndNotificationsCollector(bool useRawStatuses)
    : IMetadataCollector<GqlStatusObjectsAndNotifications>
{
    private const string StatusesKey = "statuses";
    private const string NotificationsKey = "notifications";
    public GqlStatusObjectsAndNotifications Collected { get; private set; }

    /// <inheritdoc/>
    object IMetadataCollector.Collected => Collected;

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata == null)
        {
            return;
        }

        var notifications = ConvertObjects(metadata, NotificationsKey, ConvertNotification);
        var statuses = ConvertObjects(metadata, StatusesKey, ConvertStatus);
        if (statuses == null && notifications != null && !useRawStatuses)
        {
            statuses = ConvertObjects(metadata, NotificationsKey, ConvertNotificationValuesToStatus);
        }
        if (notifications == null && statuses != null)
        {
            notifications = ConvertObjects(metadata, StatusesKey, ConvertStatusValuesToNotification);
        }
        
        Collected = new GqlStatusObjectsAndNotifications(notifications, statuses, useRawStatuses);
    }

    private static IList<T> ConvertObjects<T>(IDictionary<string, object> metadata, string key, Func<IDictionary<string, object>, T> parse)
    {
        if (metadata.TryGetValue(key, out var x) && x is IList<object> statuses)
        {
            return statuses
                .OfType<IDictionary<string, object>>()
                .Select(parse)
                .Where(y => y is not null)
                .ToList();
        }

        return null;
    }

    private static INotification ConvertNotification(IDictionary<string, object> notification)
    {
        var code = notification.GetValue("code", string.Empty);
        var title = notification.GetValue("title", string.Empty);
        var description = notification.GetValue("description", string.Empty);
        var severity = notification.GetValue("severity", string.Empty);
        var category = notification.GetValue("category", default(string));
        var position = InputPosition.ConvertFromDictionary(notification, "position");
        
        return new Notification(
            code,
            title,
            description,
            position,
            severity,
            category);
    }

    private static IGqlStatusObject ConvertStatus(IDictionary<string, object> gqlStatus)
    {
        var status = gqlStatus.GetValue<string>("gql_status", null);
        var description = gqlStatus.GetValue<string>("status_description", null);
        var diagnosticRecord = CreateDiagnosticRecord(gqlStatus);
        var position = InputPosition.ConvertFromDictionary(diagnosticRecord, "_position");
        var severity = diagnosticRecord.GetValue<string>("_severity", null);
        var classification = diagnosticRecord.GetValue<string>("_classification", null);
        var title = gqlStatus.GetValue<string>("title", null);
        var isNotification = !string.IsNullOrEmpty(gqlStatus.GetValue<string>("neo4j_code", null));

        return new GqlStatusObject(
            status,
            description,
            position,
            classification,
            severity,
            diagnosticRecord,
            title,
            isNotification);
    }

    private static IGqlStatusObject ConvertNotificationValuesToStatus(IDictionary<string, object> notification)
    {
        var severity = notification.GetValue<string>("severity", null);
        var isWarning = string.Equals(
            severity,
            nameof(NotificationSeverity.Warning),
            StringComparison.InvariantCultureIgnoreCase);
        var status = isWarning
            ? "01N42"
            : "03N42";
        
        var code = notification.GetValue<string>("code", null);
        var description = notification.GetValue<string>("description", null) ??
            (isWarning
                ? "warn: unknown warning"
                : "info: unknown notification");

        var category = notification.GetValue<string>("category", null);
        var position = InputPosition.ConvertFromDictionary(notification, "position");
        var diagnosticRecord = NewDefaultDiagnosticRecord();
        if (position != null)
        {
            diagnosticRecord["_position"] = notification["position"];
        }
        if (severity != null)
        {
            diagnosticRecord["_severity"] = severity;
        }
        if (category != null)
        {
            diagnosticRecord["_classification"] = category;
        }

        var title = notification.GetValue<string>("title", null);
        
        return new GqlStatusObject(
            status,
            description,
            position,
            category,
            severity,
            diagnosticRecord,
            title,
            !string.IsNullOrEmpty(code));
    }

    private static INotification ConvertStatusValuesToNotification(IDictionary<string, object> gqlStatus)
    {
        var code = gqlStatus.GetValue<string>("neo4j_code", null);
        if (code == null)
        {
            return null;
        }
        var title = gqlStatus.GetValue("title", string.Empty);
        var description = gqlStatus.GetValue("description", string.Empty);
        var diagnosticRecord = CreateDiagnosticRecord(gqlStatus);
        var position = InputPosition.ConvertFromDictionary(diagnosticRecord, "_position");

        var severity = diagnosticRecord.GetValue("_severity", string.Empty);
        var classification = diagnosticRecord.GetValue<string>("_classification", null);

        return new Notification(
            code,
            title,
            description,
            position,
            severity,
            classification);
    }

    private static Dictionary<string, object> CreateDiagnosticRecord(IDictionary<string, object> statusDictionary)
    {
        var diagnosticRecord = NewDefaultDiagnosticRecord();
        if (statusDictionary.TryGetValue("diagnostic_record", out var x) && x is IDictionary<string, object> readValues)
        {
            foreach (var kvp in readValues)
            {
                diagnosticRecord[kvp.Key] = kvp.Value;
            }
        }

        return diagnosticRecord;
    }

    private static Dictionary<string, object> NewDefaultDiagnosticRecord()
    {
        return new Dictionary<string, object>
        {
            ["CURRENT_SCHEMA"] = "/",
            ["OPERATION"] = "",
            ["OPERATION_CODE"] = "0"
        };
    }
}
