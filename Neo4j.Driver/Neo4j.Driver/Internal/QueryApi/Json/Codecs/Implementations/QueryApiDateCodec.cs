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
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;
using static Neo4j.Driver.Internal.QueryApi.QueryApiTemporalCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

internal sealed class QueryApiDateCodec : IQueryApiTypeCodec
{
    private static readonly Regex DateRegex =
        new("^" + DatePattern + "$", RegexOptions.Compiled);

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

        return new LocalDate(
            ParseInt(match.Groups["year"]),
            ParseInt(match.Groups["month"]),
            ParseInt(match.Groups["day"]));
    }

    public bool CanWrite(object? value) => value is LocalDate;

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var date = (LocalDate)value!;
        return CreateTypedEnvelope("Date", JsonValue.Create(FormatDate(date.Year, date.Month, date.Day)));
    }
}
