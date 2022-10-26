using System;

namespace Neo4j.Driver.Tests.TestBackend;

internal static class NotificationFilterParsing
{
    public static NotificationFilter ExtractNotificationFilter(string filterText)
    {
        if (filterText.ToLower() == "none")
            return NotificationFilter.None;

        var split = filterText.ToLower().Split('.', StringSplitOptions.RemoveEmptyEntries);
        var sev = split[0];
        var cat = split[1];

        if (sev == "all" && cat == "all")
            return NotificationFilter.All;

        return Enum.TryParse(sev + cat, true, out NotificationFilter val)
            ? val
            : throw new ArgumentOutOfRangeException();
    }
}