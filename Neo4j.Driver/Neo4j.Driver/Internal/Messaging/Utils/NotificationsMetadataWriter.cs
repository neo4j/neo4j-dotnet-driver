// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.Messaging.Utils;

internal static class NotificationsMetadataWriter
{
    private const string MinimumSeverityKey = "notifications_minimum_severity";
    private const string DisabledCategoriesKey = "notifications_disabled_categories";
    private const string AllNotificationsDisabledValue = "OFF";

    internal static void AddNotificationsConfigToMetadata(
        IDictionary<string, object> metadata,
        INotificationsConfig notificationsConfig)
    {
        switch (notificationsConfig)
        {
            case NotificationsDisabledConfig:
                metadata.Add(MinimumSeverityKey, AllNotificationsDisabledValue);
                break;

            case NotificationsConfig config:
                if (config.MinimumSeverity.HasValue)
                {
                    var severity = config.MinimumSeverity.Value.ToString().ToUpperInvariant();
                    metadata.Add(MinimumSeverityKey, severity);
                }

                if (config.DisabledCategories != null)
                {
                    var cats = config.DisabledCategories.Select(x => x.ToString().ToUpperInvariant()).ToArray();
                    metadata.Add(DisabledCategoriesKey, cats);
                }

                break;

            default:
                return;
        }
    }
}
