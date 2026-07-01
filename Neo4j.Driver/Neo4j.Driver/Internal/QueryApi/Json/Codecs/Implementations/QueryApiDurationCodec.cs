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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;
using static Neo4j.Driver.Internal.QueryApi.QueryApiTemporalCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiDurationCodec : IQueryApiTypeCodec
{
    private const long NanosPerSecond = 1_000_000_000;

    private static readonly Regex DurationRegex = new(
        "^P" +
        @"(?:(?<years>[+-]?\d+)Y)?" +
        @"(?:(?<months>[+-]?\d+)M)?" +
        @"(?:(?<weeks>[+-]?\d+)W)?" +
        @"(?:(?<days>[+-]?\d+)D)?" +
        "(?:T" +
        @"(?:(?<hours>[+-]?\d+)H)?" +
        @"(?:(?<minutes>[+-]?\d+)M)?" +
        @"(?:(?<seconds>[+-]?\d+)(?:[.,](?<fraction>\d{1,9}))?S)?" +
        ")?$",
        RegexOptions.Compiled);

    public bool CanRead(string typeName)
    {
        return typeName == "Duration";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value").GetString()
            ?? throw new ProtocolException("Duration value was null.");

        var match = DurationRegex.Match(value);
        if (!match.Success)
        {
            throw new ProtocolException($"Duration value '{value}' is not a valid ISO-8601 duration.");
        }

        var months = ParseOptionalLong(match.Groups["years"]) * 12 + ParseOptionalLong(match.Groups["months"]);
        var days = ParseOptionalLong(match.Groups["weeks"]) * 7 + ParseOptionalLong(match.Groups["days"]);
        var seconds = 
            ParseOptionalLong(match.Groups["hours"]) * 3600 +
            ParseOptionalLong(match.Groups["minutes"]) * 60 +
            ParseOptionalLong(match.Groups["seconds"]);

        var nanos = ParseFractionAsNanoseconds(match.Groups["fraction"]);
        if (match.Groups["seconds"].Value.StartsWith('-'))
        {
            nanos = -nanos;
        }

        if (nanos < 0)
        {
            seconds -= 1;
            nanos += (int)NanosPerSecond;
        }

        return new Duration(months, days, seconds, nanos);
    }

    public bool CanWrite(object? value) => value is Duration;

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var duration = (Duration)value!;
        var months = duration.Months.ToString(CultureInfo.InvariantCulture);
        var days = duration.Days.ToString(CultureInfo.InvariantCulture);
        return CreateTypedEnvelope(
            "Duration",
            JsonValue.Create($"P{months}M{days}DT{FormatSeconds(duration.Seconds, duration.Nanos)}S"));
    }

    private static string FormatSeconds(long seconds, int nanos)
    {
        if (nanos == 0)
        {
            return seconds.ToString(CultureInfo.InvariantCulture);
        }

        var negative = seconds < 0;
        var wholeSeconds = negative ? -(seconds + 1) : seconds;
        var fraction = negative ? (int)NanosPerSecond - nanos : nanos;
        var sign = negative ? "-" : string.Empty;

        return 
            $"{sign}{wholeSeconds.ToString(CultureInfo.InvariantCulture)}." +
            $"{fraction.ToString("D9", CultureInfo.InvariantCulture)}";
    }

    private static long ParseOptionalLong(Group group)
    {
        return group.Success ? long.Parse(group.Value, CultureInfo.InvariantCulture) : 0;
    }
}
