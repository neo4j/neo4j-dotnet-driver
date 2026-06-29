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

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiLocalTimeCodec : IQueryApiTypeCodec
{
    private static readonly Regex LocalTimeRegex =
        new(
            @"^(?<hour>\d{2}):(?<minute>\d{2})(?::(?<second>\d{2})(?:\.(?<fraction>\d{1,9}))?)?$",
            RegexOptions.Compiled);

    public bool CanRead(string typeName) => typeName == "LocalTime";

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value").GetString() ??
            throw new ProtocolException("LocalTime value was null.");

        var match = LocalTimeRegex.Match(value);
        if (!match.Success)
        {
            throw new ProtocolException($"LocalTime value '{value}' is not a valid ISO-8601 time.");
        }

        var hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture);
        var second = ParseOptional(match.Groups["second"]);
        var nanosecond = ParseFraction(match.Groups["fraction"]);

        return new LocalTime(hour, minute, second, nanosecond);
    }

    public bool CanWrite(object? value) => value is LocalTime;

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var time = (LocalTime)value!;
        var fraction = time.Nanosecond > 0
            ? $".{time.Nanosecond:D9}"
            : string.Empty;

        var text = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}{fraction}";

        return CreateTypedEnvelope("LocalTime", JsonValue.Create(text));
    }

    private static int ParseOptional(Group group)
    {
        if (!group.Success)
        {
            return 0;
        }

        return int.Parse(group.Value, CultureInfo.InvariantCulture);
    }

    private static int ParseFraction(Group group)
    {
        if (!group.Success)
        {
            return 0;
        }

        var fractionWithPadding = group.Value.PadRight(9, '0');
        return int.Parse(fractionWithPadding, CultureInfo.InvariantCulture);
    }
}
