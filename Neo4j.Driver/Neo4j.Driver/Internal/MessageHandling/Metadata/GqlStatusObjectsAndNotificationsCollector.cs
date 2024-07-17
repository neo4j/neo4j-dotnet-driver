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

internal class GqlStatusObjectsAndNotificationsCollector : IMetadataCollector<GqlStatusObjectsAndNotifications>
{
    private bool _legacyNotifications;

    public GqlStatusObjectsAndNotificationsCollector(bool legacyNotifications)
    {
        _legacyNotifications = legacyNotifications;
    }

    public GqlStatusObjectsAndNotifications Collected { get; private set; }

    /// <inheritdoc />
    object IMetadataCollector.Collected => Collected;

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata == null)
        {
            return;
        }

        IEnumerable<IGqlStatusObject> gqlStatusObjects;
        IEnumerable<INotification> notifications;
        if (_legacyNotifications)
        {
            var objects = ExtractGqlStatusObjectsFromNotifications(metadata).ToList();
            gqlStatusObjects = objects.Where(x => x is not null);
            notifications = Enumerable.Empty<INotification>();
        }
        else
        {
            var objects = ExtractGqlStatusObjects(metadata).ToList();
            gqlStatusObjects = objects;
            notifications = objects.OfType<Notification>();
        }

        Collected = new GqlStatusObjectsAndNotifications(notifications.ToList(), gqlStatusObjects.ToList());
    }

    public static IEnumerable<INotification> ExtractGqlStatusObjectsFromNotifications(
        IDictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("notifications", out var notificationsValue) &&
            notificationsValue is IDictionary<string, object> notificationsDict)
        {
            return notificationsDict.Select(
                kvp =>
                {
                    var value = (IDictionary<string, object>)kvp.Value;
                    var code = value["code"].ToString();
                    var title = value["title"].ToString();
                    var description = value["description"].ToString();
                    var rawSeverityLevel = value.TryGetValue("severity", out var sev) ? sev.ToString() : null;
                    var severityLevel = rawSeverityLevel == null
                        ? NotificationSeverity.Unknown
                        : (NotificationSeverity)Enum.Parse(typeof(NotificationSeverity), rawSeverityLevel, true);

                    var rawCategory = value.TryGetValue("category", out var cat) ? cat.ToString() : null;

                    var posValue = (IDictionary<string, object>)value["position"];
                    InputPosition position = null;
                    if (posValue != null)
                    {
                        position = new InputPosition(
                            posValue["offset"].As<int>(),
                            posValue["line"].As<int>(),
                            posValue["column"].As<int>());
                    }

                    var gqlStatusCode = "03N42";
                    var gqlStatusDescription = description;
                    if (severityLevel == NotificationSeverity.Warning)
                    {
                        gqlStatusCode = "01N42";

                        if (gqlStatusDescription is null or "" or "null")
                        {
                            gqlStatusDescription = "warn: unknown warning";
                        }
                    }
                    else
                    {
                        if (gqlStatusDescription is null or "" or "null")
                        {
                            gqlStatusDescription = "info: unknown notification";
                        }
                    }

                    var diagnosticRecord = new Dictionary<string, object>(3)
                    {
                        { "OPERATION", "" },
                        { "OPERATION_CODE", "0" },
                        { "CURRENT_SCHEMA", "/" }
                    };

                    if (rawSeverityLevel != null)
                    {
                        diagnosticRecord["_severity"] = rawSeverityLevel;
                    }

                    if (rawCategory != null)
                    {
                        diagnosticRecord["_classification"] = rawCategory;
                    }

                    if (position != null)
                    {
                        diagnosticRecord["_position"] = new Dictionary<string, object>
                        {
                            { "offset", position.Offset },
                            { "line", position.Line },
                            { "column", position.Column }
                        };
                    }

                    return new Notification(
                        gqlStatusCode,
                        gqlStatusDescription,
                        diagnosticRecord,
                        code,
                        title,
                        description,
                        position,
                        rawSeverityLevel,
                        rawCategory);
                });
        }

        return Enumerable.Empty<Notification>();
    }

    private static IEnumerable<IGqlStatusObject> ExtractGqlStatusObjects(IDictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("statuses", out var statuses) && statuses is IList<object> statusList)
        {
            var statusObjectList = statusList.Cast<IDictionary<string, object>>();
            return statusObjectList.Select(ExtractGqlStatusObject);
        }

        return null;
    }

    private static GqlStatusObject ExtractGqlStatusObject(object value)
    {
        var valueDict = (IDictionary<string, object>)value;
        var status = valueDict["gql_status"].ToString();
        var description = valueDict["status_description"].ToString();
        IDictionary<string, object> diagnosticRecord = GqlStatusObject.DefaultDiagnosticRecord;
        var diagnosticRecordObject = valueDict["diagnostic_record"];
        if (diagnosticRecordObject is IDictionary<string, object> diagnosticRecordValue)
        {
            diagnosticRecord.ApplyValues(diagnosticRecordValue);
        }

        // if there is no neo4j_code, then we create a GqlStatusObject rather than a notification
        var codeFound = valueDict.TryGetValue("neo4j_code", "", out var neo4jCode);
        if (!codeFound)
        {
            return new GqlStatusObject(status, description, diagnosticRecord);
        }

        // otherwise, we have an old-style notification
        var title = valueDict["title"].ToString();
        var positionFound = diagnosticRecord.TryGetValue("_position", out var positionObj);
        InputPosition position = null;
        if (positionFound && positionObj is IDictionary<string, object> positionValue)
        {
            var offsetFound = positionValue.TryGetValue("offset", 0, out var offset);
            var lineFound = positionValue.TryGetValue("line", 0, out var line);
            var columnFound = positionValue.TryGetValue("column", 0, out var column);
            if (offsetFound && lineFound && columnFound)
            {
                position = new InputPosition(offset, line, column);
            }
        }

        diagnosticRecord.TryGetValue<string>("_severity", null, out var rawSeverity);
        diagnosticRecord.TryGetValue<string>("_classification", null, out var rawClassification);

        return new Notification(
            status,
            description,
            diagnosticRecord,
            neo4jCode,
            title,
            description,
            position,
            rawSeverity,
            rawClassification);
    }
}

internal record GqlStatusObjectsAndNotifications(
    IList<INotification> Notifications,
    IList<IGqlStatusObject> GqlStatusObjects);
