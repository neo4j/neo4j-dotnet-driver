using System;
using Neo4j.Driver.Internal.Types;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Messaging.V5_1;

internal static class NotificationFiltersMetadata
{
    internal static void SetNotificationFiltersOnMetadata(IDictionary<string, object> metaData,
        INotificationFilterConfig notificationFilters)
    {
        if (notificationFilters is null)
            return;

        var filters = notificationFilters switch
        {
            NoNotificationFilterConfig => Array.Empty<string>(),
            NotificationFilterSetConfig notificationFilterSet => notificationFilterSet.Filters.Select(StringifyFilter).ToArray(),
            _ => null
        };

        metaData.Add("notifications", filters);
    }

    private static string StringifyFilter((Severity, Category) x)
    {
        var severity = x.Item1 == Severity.All ? "*" : x.Item1.ToString();
        var category = x.Item2 == Category.All ? "*" : x.Item2.ToString();

        return string.Concat(severity, ".", category).ToUpperInvariant();
    }
}