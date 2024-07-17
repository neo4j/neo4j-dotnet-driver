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

internal class NotificationsCollector : IMetadataCollector<IList<INotification>>
{
    internal const string NotificationsKey = "notifications";

    object IMetadataCollector.Collected => Collected;

    public IList<INotification> Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata != null && metadata.TryGetValue(NotificationsKey, out var notificationsValue))
        {
            switch (notificationsValue)
            {
                case null:
                    Collected = null;
                    break;

                case List<object> notificationsList:
                    Collected = notificationsList.Cast<IDictionary<string, object>>()
                        .Select(CollectNotification)
                        .ToList();

                    break;

                default:
                    throw new ProtocolException(
                        $"Expected '{NotificationsKey}' metadata to be of type 'List<Object>', but got '{notificationsValue?.GetType().Name}'.");
            }
        }
    }

    private static INotification CollectNotification(IDictionary<string, object> notificationDict)
    {
        var code = notificationDict.GetValue("code", string.Empty);
        var title = notificationDict.GetValue("title", string.Empty);
        var description = notificationDict.GetValue("description", string.Empty);
        var posValue = notificationDict.GetValue("position", new Dictionary<string, object>());
        var severity = notificationDict.GetValue("severity", string.Empty);
        var category = notificationDict.GetValue<string>("category", null);
        var foundOffset = posValue.TryGetValue("offset", 0L, out var offset);
        var foundLine = posValue.TryGetValue("line", 0L, out var line);
        var foundColumn = posValue.TryGetValue("column", 0L, out var column);

        var position = foundOffset || foundLine || foundColumn
            ? new InputPosition((int)offset, (int)line, (int)column)
            : null;

        var gqlStatusCode = "03N42";
        var gqlStatusDescription = description;
        if (Notification.ParseSeverity(severity) == NotificationSeverity.Warning)
        {
            gqlStatusCode = "01N42";
            if (gqlStatusDescription is null or "null")
            {
                gqlStatusDescription = "warning: unknown warning";
            }
        }
        else
        {
            if (gqlStatusDescription is null or "null")
            {
                gqlStatusDescription = "info: unknown notification";
            }
        }

        var diagnosticRecord = new Dictionary<string, object>(3)
        {
            ["OPERATION"] = "",
            ["OPERATION_CODE"] = "0",
            ["CURRENT_SCHEMA"] = "/"
        };

        return new Notification(
            gqlStatusCode,
            gqlStatusDescription,
            diagnosticRecord,
            code,
            title,
            description,
            position,
            severity,
            category);
    }
}
