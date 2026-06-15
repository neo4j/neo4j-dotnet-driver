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
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Serializes query parameter values to the HTTP Query API v1.0 typed JSON format:
/// <c>{"$type":"...", "_value":...}</c>.
/// Registered on the <see cref="JsonSerializerOptions"/> used for request body serialization only.
/// </summary>
internal sealed class QueryApiParameterConverter : JsonConverter<object?>
{
    public override bool HandleNull => true;

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("QueryApiParameterConverter is write-only.");

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                WriteTyped(writer, "Null", w => w.WriteNullValue());
                break;

            case bool b:
                WriteTyped(writer, "Boolean", w => w.WriteBooleanValue(b));
                break;

            case long l:
                WriteTyped(writer, "Integer", w => w.WriteStringValue(l.ToString()));
                break;

            case int i:
                WriteTyped(writer, "Integer", w => w.WriteStringValue(i.ToString()));
                break;

            case short s:
                WriteTyped(writer, "Integer", w => w.WriteStringValue(s.ToString()));
                break;

            case sbyte sb:
                WriteTyped(writer, "Integer", w => w.WriteStringValue(sb.ToString()));
                break;

            case double d:
                WriteTyped(writer, "Float", w => w.WriteStringValue(FormatFloat(d)));
                break;

            case float f:
                WriteTyped(writer, "Float", w => w.WriteStringValue(FormatFloat(f)));
                break;

            case string str:
                WriteTyped(writer, "String", w => w.WriteStringValue(str));
                break;

            case byte[] bytes:
                WriteTyped(writer, "Base64", w => w.WriteStringValue(Convert.ToBase64String(bytes)));
                break;

            case IDictionary<string, object?> map:
                WriteTyped(writer, "Map", w =>
                {
                    w.WriteStartObject();
                    foreach (var (k, v) in map)
                    {
                        w.WritePropertyName(k);
                        Write(w, v, options);
                    }

                    w.WriteEndObject();
                });
                break;

            case IDictionary dict:
                WriteTyped(writer, "Map", w =>
                {
                    w.WriteStartObject();
                    foreach (DictionaryEntry entry in dict)
                    {
                        w.WritePropertyName(entry.Key.ToString()!);
                        Write(w, entry.Value, options);
                    }

                    w.WriteEndObject();
                });
                break;

            case IEnumerable list:
                WriteTyped(writer, "List", w =>
                {
                    w.WriteStartArray();
                    foreach (var item in list)
                    {
                        Write(w, item, options);
                    }
                    
                    w.WriteEndArray();
                });
                break;

            default:
                throw new NotSupportedException(
                    $"Query parameter type '{value.GetType().Name}' is not supported by the HTTP Query API.");
        }
    }

    private static void WriteTyped(Utf8JsonWriter writer, string typeName, Action<Utf8JsonWriter> writeValue)
    {
        writer.WriteStartObject();
        writer.WriteString("$type", typeName);
        writer.WritePropertyName("_value");
        writeValue(writer);
        writer.WriteEndObject();
    }

    private static string FormatFloat(double value) =>
        double.IsNaN(value) ? "NaN"
        : double.IsPositiveInfinity(value) ? "Infinity"
        : double.IsNegativeInfinity(value) ? "-Infinity"
        : value.ToString("G17");
}
