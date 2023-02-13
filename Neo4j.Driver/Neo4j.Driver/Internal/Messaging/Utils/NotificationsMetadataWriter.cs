using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.Messaging.Utils;

internal static class NotificationsMetadataWriter
{
    private const string MinimumSeverityKey = "notifications_minimum_severity";
    private const string DisabledCategoriesKey = "notifications_disabled_categories";

    internal static void AddNotificationsConfigToMetadata(
        IDictionary<string, object> metadata,
        INotificationsConfig notificationsConfig)
    {
        switch (notificationsConfig)
        {
            case NotificationsDisabledConfig:
                metadata.Add(MinimumSeverityKey, "OFF");
                break;

            case NotificationsConfig config:
                if (config.MinimumSeverity.HasValue)
                {
                    var severity = config.MinimumSeverity.Value.ToString().ToUpperInvariant();
                    metadata.Add(MinimumSeverityKey, severity);
                }

                if (config.DisabledCategories?.Any() ?? false)
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
