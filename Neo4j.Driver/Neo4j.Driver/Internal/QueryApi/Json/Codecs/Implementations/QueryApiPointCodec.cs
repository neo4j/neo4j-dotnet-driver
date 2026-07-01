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
internal sealed class QueryApiPointCodec : IQueryApiTypeCodec
{
    // One coordinate: optional +/-, then Infinity/NaN or a decimal with optional fraction and exponent
    private const string Float = @"[-+]?(?:Infinity|NaN|(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?)";

    // SRID=<int>;POINT (<x> <y>) or SRID=<int>;POINT Z (<x> <y> <z>); z optional, whitespace lenient
    private static readonly Regex PointRegex = new(
        $@"^SRID=(?<srid>\d+);\s*POINT(?:\s+Z)?\s*\(" +
        $@"\s*(?<x>{Float})\s+(?<y>{Float})(?:\s+(?<z>{Float}))?\s*\)$",
        RegexOptions.Compiled);

    public bool CanRead(string typeName) => typeName == "Point";

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value").GetString()
            ?? throw new ProtocolException("Point value was null.");

        var match = PointRegex.Match(value);
        if (!match.Success)
        {
            throw new ProtocolException($"Point value '{value}' is not a valid WKT point.");
        }

        var srId = int.Parse(match.Groups["srid"].Value, CultureInfo.InvariantCulture);
        var x = ParseFloat(match.Groups["x"].Value);
        var y = ParseFloat(match.Groups["y"].Value);

        return match.Groups["z"].Success
            ? new Point(srId, x, y, ParseFloat(match.Groups["z"].Value))
            : new Point(srId, x, y);
    }

    public bool CanWrite(object? value)
    {
        return value is Point;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var point = (Point)value!;
        var srId = point.SrId.ToString(CultureInfo.InvariantCulture);
        var wire = point.Dimension == Point.ThreeD
            ? $"SRID={srId};POINT Z ({FormatFloat(point.X)} {FormatFloat(point.Y)} {FormatFloat(point.Z)})"
            : $"SRID={srId};POINT ({FormatFloat(point.X)} {FormatFloat(point.Y)})";

        return CreateTypedEnvelope("Point", JsonValue.Create(wire));
    }
}
