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

#nullable enable

using System.Globalization;
using System.Text.RegularExpressions;

namespace Neo4j.Driver.Internal.QueryApi;

internal static class QueryApiTemporalCodecHelper
{
    public const string DatePattern = @"(?<year>[+-]?\d+)-(?<month>\d{2})-(?<day>\d{2})";

    public const string TimePattern =
        @"(?<hour>\d{2}):(?<minute>\d{2})(?::(?<second>\d{2})(?:\.(?<fraction>\d{1,9}))?)?";

    public static string FormatDate(int year, int month, int day)
    {
        return $"{FormatYear(year)}-{month:D2}-{day:D2}";
    }

    public static string FormatTime(int hour, int minute, int second, int nanosecond)
    {
        var fraction = nanosecond > 0 ? $".{nanosecond:D9}" : string.Empty;
        return $"{hour:D2}:{minute:D2}:{second:D2}{fraction}";
    }

    public static int ParseInt(Group group)
    {
        return int.Parse(group.Value, CultureInfo.InvariantCulture);
    }

    public static int ParseOptionalInt(Group group)
    {
        return group.Success ? ParseInt(group) : 0;
    }

    public static int ParseFractionAsNanoseconds(Group group)
    {
        return group.Success
            ? int.Parse(group.Value.PadRight(9, '0'), CultureInfo.InvariantCulture)
            : 0;
    }

    private static string FormatYear(int year)
    {
        var prefix = year > 9999 ? "+" : string.Empty;
        var formattedYear = year.ToString("D4", CultureInfo.InvariantCulture);
        return prefix + formattedYear;
    }
}
