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
using System.Text.Json.Nodes;

namespace Neo4j.Driver.Internal.QueryApi;

internal static class QueryApiCodecHelper
{
    internal static JsonObject CreateTypedEnvelope(string type, JsonNode? valueNode) =>
        new() { ["$type"] = type, ["_value"] = valueNode };

    internal static string FormatFloat(double value) => value switch
    {
        double.NaN => "NaN",
        double.PositiveInfinity => "Infinity",
        double.NegativeInfinity => "-Infinity",
        _ => value.ToString("G17", CultureInfo.InvariantCulture)
    };

    internal static double ParseFloat(string value) => value switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        _ => double.Parse(value, CultureInfo.InvariantCulture)
    };
}
