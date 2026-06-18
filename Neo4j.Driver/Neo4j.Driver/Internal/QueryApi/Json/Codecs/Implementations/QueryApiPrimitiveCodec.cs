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
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Bidirectional codec for the HTTP Query API primitive types: <c>Null</c>, <c>Boolean</c>, <c>Integer</c>,
/// <c>Float</c>, <c>String</c> and <c>Base64</c>. Integers and floats travel as JSON strings; bytes as Base64.
/// <c>Unsupported</c> is read-only (the server emits it for values the API cannot represent).
/// </summary>
[AutoRegister]
internal sealed class QueryApiPrimitiveCodec : IQueryApiTypeCodec
{
    private static readonly HashSet<string> ReadableTypes =
        ["Null", "Boolean", "Integer", "Float", "String", "Base64", "Unsupported"];

    private readonly IBase64Encoder _encoder;
    private readonly IBase64Decoder _decoder;

    public QueryApiPrimitiveCodec(IBase64Encoder encoder, IBase64Decoder decoder)
    {
        _encoder = encoder;
        _decoder = decoder;
    }

    public bool CanRead(string typeName) => ReadableTypes.Contains(typeName);

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var typeName = element.GetProperty("$type").GetString()!;
        var value = element.GetProperty("_value");

        return typeName switch
        {
            "Null" => null,
            "Boolean" => value.GetBoolean(),
            "Integer" => long.Parse(value.GetString()!, CultureInfo.InvariantCulture),
            "Float" => ParseFloat(value.GetString()!),
            "String" => value.GetString(),
            "Base64" => _decoder.Decode(value.GetString()!),
            "Unsupported" => new UnsupportedType("Unsupported", 0, 0, value.GetString()!),
            _ => throw new NotSupportedException($"Unsupported Neo4j type: {typeName}")
        };
    }

    public bool CanWrite(object? value)
    {
        return value is
            null
            or bool
            or double or float
            or long or int or short or sbyte
            or string
            or byte[];
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        return value switch
        {
            null => CreateTypedEnvelope("Null", null),
            bool b => CreateTypedEnvelope("Boolean", JsonValue.Create(b)),
            double d => CreateTypedEnvelope("Float", JsonValue.Create(FormatFloat(d))),
            float f => CreateTypedEnvelope("Float", JsonValue.Create(FormatFloat(f))),
            long or int or short or sbyte => CreateTypedEnvelope("Integer", JsonValue.Create(FormatInteger(value))),
            string s => CreateTypedEnvelope("String", JsonValue.Create(s)),
            byte[] bytes => CreateTypedEnvelope("Base64", JsonValue.Create(_encoder.Encode(bytes))),
            _ => throw new InvalidOperationException($"Unsupported type: {value.GetType().Name}")
        };
    }

    private static double ParseFloat(string value) => value switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        _ => double.Parse(value, CultureInfo.InvariantCulture)
    };

    private static string FormatFloat(double value)
    {
        return value switch
        {
            double.NaN => "NaN",
            double.PositiveInfinity => "Infinity",
            double.NegativeInfinity => "-Infinity",
            _ => value.ToString("G17", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatInteger(object value)
    {
        return Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture);
    }
}
