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

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo4j.Driver.TestKitBackend.Serialization;

internal interface IStoredObjectFieldTransformer
{
    string Transform(string dataJson, Type targetType);
}

internal class StoredObjectFieldTransformer : IStoredObjectFieldTransformer
{
    private record HandleField(string PlaceholderName, string IdFieldName, bool KeepIdField);

    private readonly ConcurrentDictionary<Type, HandleField[]> _handleFields = new();

    public string Transform(string dataJson, Type targetType)
    {
        var handleFields = _handleFields.GetOrAdd(targetType, FindHandleFields);
        if (handleFields.Length == 0)
        {
            return dataJson;
        }

        var data = JsonNode.Parse(dataJson) as JsonObject
            ?? throw new TestKitProtocolException($"The data of \"{targetType.Name}\" must be a JSON object.");

        foreach (var field in handleFields)
        {
            if (!data.TryGetPropertyValue(field.IdFieldName, out var idValue))
            {
                continue;
            }

            if (field.KeepIdField)
            {
                data[field.PlaceholderName] = idValue?.DeepClone();
            }
            else
            {
                data.Remove(field.IdFieldName);
                data[field.PlaceholderName] = idValue;
            }
        }

        return data.ToJsonString();
    }

    private static HandleField[] FindHandleFields(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => (Property: property, Attribute: property.GetCustomAttribute<StoredObjectAttribute>()))
            .Where(pair => pair.Attribute is not null)
            .Select(pair => new HandleField(
                JsonNamingPolicy.CamelCase.ConvertName(pair.Property.Name),
                pair.Attribute!.IdFieldName ?? JsonNamingPolicy.CamelCase.ConvertName(pair.Property.Name) + "Id",
                type.GetProperty(pair.Property.Name + "Id") is not null))
            .ToArray();
    }
}
