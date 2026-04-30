using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;

namespace Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;

internal class JsonObjectConverter : IJsonObjectConverter
{
    private readonly IEnumerable<ITypedJsonElementConverter> _typedConverters;

    public JsonObjectConverter(IEnumerable<ITypedJsonElementConverter> typedConverters)
    {
        _typedConverters = typedConverters;
    }

    public object Convert(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number => jsonElement.TryGetInt64(out var l) ? (object)l : jsonElement.GetDouble(),
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(Convert).ToList(),
            JsonValueKind.Object => ConvertObject(jsonElement),
            _ => throw new ArgumentOutOfRangeException(
                nameof(jsonElement),
                jsonElement.ValueKind,
                "Unexpected JSON value kind.")
        };
    }

    public object ConvertObject(JsonElement jsonElement)
    {
        if (jsonElement.TryGetProperty("$type", out var typeElement))
        {
            var typeName = typeElement.GetString() ?? "unknown";

            foreach (var converter in _typedConverters)
            {
                if (converter.CanConvert(typeName))
                {
                    return converter.Convert(jsonElement);
                }
            }

            return $"Unsupported type: {typeName}";
        }

        var dict = new Dictionary<string, object?>();
        foreach (var prop in jsonElement.EnumerateObject())
        {
            dict[prop.Name] = Convert(prop.Value);
        }

        return dict;
    }
}
