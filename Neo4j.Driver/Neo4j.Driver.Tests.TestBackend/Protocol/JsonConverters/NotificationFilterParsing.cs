using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend;

internal static class NotificationFilterParsing
{
    public static (Severity, Category)[] Parse(IEnumerable<string> filters)
    {
        return filters.Select(x =>
        {
            var tuple = x.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var sev = tuple[0];
            var cat = tuple[1];
            return (
                Enum.Parse<Severity>(sev, true), 
                Enum.Parse<Category>(cat, true));
        }).ToArray();
    }
}