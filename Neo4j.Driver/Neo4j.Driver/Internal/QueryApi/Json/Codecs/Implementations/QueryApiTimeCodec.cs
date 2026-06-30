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
internal sealed class QueryApiTimeCodec : IQueryApiTypeCodec
{
    private static readonly Regex TimeRegex =
        new("^" + TimePattern + OffsetPattern + "$", RegexOptions.Compiled);

    public bool CanRead(string typeName) => typeName == "Time";

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value").GetString()
            ?? throw new ProtocolException("Time value was null.");

        var match = TimeRegex.Match(value);
        if (!match.Success)
        {
            throw new ProtocolException($"Time value '{value}' is not a valid ISO-8601 offset time.");
        }

        return new OffsetTime(
            ParseInt(match.Groups["hour"]),
            ParseInt(match.Groups["minute"]),
            ParseOptionalInt(match.Groups["second"]),
            ParseFractionAsNanoseconds(match.Groups["fraction"]),
            ParseOffset(match.Groups["offset"]));
    }

    public bool CanWrite(object? value)
    {
        return value is OffsetTime;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var time = (OffsetTime)value!;
        var text = 
            FormatTime(time.Hour, time.Minute, time.Second, time.Nanosecond) +
            FormatOffset(time.OffsetSeconds);

        return CreateTypedEnvelope("Time", JsonValue.Create(text));
    }
}
