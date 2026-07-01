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

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;
using static Neo4j.Driver.Internal.QueryApi.QueryApiTemporalCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiOffsetDateTimeCodec : IQueryApiTypeCodec
{
    private static readonly Regex OffsetDateTimeRegex =
        new("^" + DatePattern + "T" + TimePattern + OffsetPattern + "$", RegexOptions.Compiled);

    public bool CanRead(string typeName) => typeName == "OffsetDateTime";

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value").GetString()
            ?? throw new ProtocolException("OffsetDateTime value was null.");

        var match = OffsetDateTimeRegex.Match(value);
        if (!match.Success)
        {
            throw new ProtocolException($"OffsetDateTime value '{value}' is not a valid ISO-8601 offset date-time.");
        }

        var (utcSeconds, nanoseconds, offsetSeconds) = ParseUtcInstant(match);
        return new ZonedDateTime(utcSeconds, nanoseconds, Zone.Of(offsetSeconds));
    }

    public bool CanWrite(object? value)
    {
        return value is ZonedDateTime { Zone: ZoneOffset };
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var dateTime = (ZonedDateTime)value!;
        var date = FormatDate(dateTime.Year, dateTime.Month, dateTime.Day);
        var time = FormatTime(dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Nanosecond);
        var offset = FormatOffset(dateTime.OffsetSeconds);
        return CreateTypedEnvelope("OffsetDateTime", JsonValue.Create($"{date}T{time}{offset}"));
    }
}
