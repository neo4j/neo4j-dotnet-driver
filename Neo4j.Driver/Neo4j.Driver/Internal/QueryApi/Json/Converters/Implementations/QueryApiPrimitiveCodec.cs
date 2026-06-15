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
using Neo4j.Driver.Internal.DependencyInjection;

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

    private readonly IJsonEnvelopeWriter _envelope;
    private readonly IBase64Encoder _encoder;
    private readonly IBase64Decoder _decoder;

    public QueryApiPrimitiveCodec(IJsonEnvelopeWriter envelope, IBase64Encoder encoder, IBase64Decoder decoder)
    {
        _envelope = envelope;
        _encoder = encoder;
        _decoder = decoder;
    }

    public bool CanRead(string typeName) => ReadableTypes.Contains(typeName);

    public object? Read(JsonElement element, IJsonValueConverter recurse)
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

    public bool CanWrite(object? value) => value is null or bool or long or int or short or sbyte or double or float
        or string or byte[];

    public void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        _ = value switch
        {
            null => TryWriteTyped(writer, "Null", w => w.WriteNullValue()),
            bool b => TryWriteTyped(writer, "Boolean", w => w.WriteBooleanValue(b)),
            double d => TryWriteTyped(writer, "Float", w => w.WriteStringValue(FormatFloat(d))),
            float f => TryWriteTyped(writer, "Float", w => w.WriteStringValue(FormatFloat(f))),
            string str => TryWriteTyped(writer, "String", w => w.WriteStringValue(str)),
            byte[] bytes => TryWriteTyped(writer, "Base64", w => w.WriteStringValue(_encoder.Encode(bytes))),
            long or int or short or sbyte => TryWriteTyped(writer, "Integer", w => WriteInt64Str(w, value)),
            _ => throw new InvalidOperationException($"Unsupported type: {value.GetType().Name}")
        };
    }

    private static void WriteInt64Str(Utf8JsonWriter w, object? value)
    {
        var int64 = Convert.ToInt64(value);
        var asString = int64.ToString(CultureInfo.InvariantCulture);
        w.WriteStringValue(asString);
    }


    private bool TryWriteTyped(Utf8JsonWriter writer, string typeName, Action<Utf8JsonWriter> writeValue)
    {
        using (_envelope.OpenTypedEnvelope(writer, typeName))
        {
            writeValue(writer);
        }

        return true;
    }

    private static double ParseFloat(string value) => value switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        _ => double.Parse(value, CultureInfo.InvariantCulture)
    };

    private static string FormatFloat(double value) =>
        double.IsNaN(value) ? "NaN"
        : double.IsPositiveInfinity(value) ? "Infinity"
        : double.IsNegativeInfinity(value) ? "-Infinity"
        : value.ToString("G17", CultureInfo.InvariantCulture);
}
