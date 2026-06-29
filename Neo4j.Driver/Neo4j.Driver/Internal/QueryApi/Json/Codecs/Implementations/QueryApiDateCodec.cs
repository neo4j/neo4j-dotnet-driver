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

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Bidirectional codec for the HTTP Query API <c>Date</c> type. The value travels as an ISO-8601 date string
/// (<c>"yyyy-MM-dd"</c>), with an expanded year representation outside the 0..9999 range: years above 9999 are
/// prefixed with <c>+</c>, negative years carry a <c>-</c>, and all others are zero-padded to four digits.
/// </summary>
[AutoRegister]
internal sealed class QueryApiDateCodec : IQueryApiTypeCodec
{
    private static readonly Regex DateRegex =
        new(@"^(?<year>[+-]?\d+)-(?<month>\d{2})-(?<day>\d{2})$", RegexOptions.Compiled);

    public bool CanRead(string typeName) => typeName == "Date";

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value").GetString()
            ?? throw new ProtocolException("Date value was null.");

        var match = DateRegex.Match(value);
        if (!match.Success)
        {
            throw new ProtocolException($"Date value '{value}' is not a valid ISO-8601 date.");
        }

        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture);

        return new LocalDate(year, month, day);
    }

    public bool CanWrite(object? value)
    {
        return value is LocalDate;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var date = (LocalDate)value!;
        var text = $"{FormatYear(date.Year)}-{date.Month:D2}-{date.Day:D2}";
        return CreateTypedEnvelope("Date", JsonValue.Create(text));
    }

    private static string FormatYear(int year)
    {
        var prefix = year > 9999 ? "+" : string.Empty;
        var yearString = year.ToString("D4", CultureInfo.InvariantCulture);
        return $"{prefix}{yearString}";
    }
}
