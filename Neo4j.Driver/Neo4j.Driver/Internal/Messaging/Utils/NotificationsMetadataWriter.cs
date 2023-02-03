using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.Messaging.Utils;

internal static class NotificationsMetadataWriter
{
    private const string MinimumSeverityKey = "noti_min_sev";
    private const string DisabledCategoriesKey = "noti_dis_cats";

    internal static void AddNotificationsConfigToMetadata(
        IDictionary<string, object> metadata,
        INotificationsConfig notificationsConfig)
    {
        if (notificationsConfig?.Optimize ?? true)
        {
            return;
        }

        switch (notificationsConfig)
        {
            case NoNotificationsConfig:
                metadata.Add(MinimumSeverityKey, "OFF");
                break;

            case NotificationsConfig config:
                var sendSeverity = config.MinimumSeverity != Severity.Information ||
                    (config.MinimumSeverity == Severity.Information && !config.DisabledCategories.Any());

                if (sendSeverity)
                {
                    var severity = config.MinimumSeverity.ToString().ToUpperInvariant();
                    metadata.Add(MinimumSeverityKey, severity);
                }

                if (config.DisabledCategories.Any())
                {
                    var cats = config.DisabledCategories.Select(x => x.ToString().ToUpperInvariant()).ToArray();
                    metadata.Add(DisabledCategoriesKey, cats);
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(notificationsConfig));
        }
    }
}
