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

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Serialization;

[RegistrationLifetime(RegistrationLifetime.Singleton)]
internal class JsonOptionsProvider : IJsonOptionsProvider
{
    private readonly JsonSerializerOptions _options;
    private readonly IObjectStoreAccessor _objectStoreAccessor;

    public JsonOptionsProvider(IProtocolJsonConverter[] converters, IObjectStoreAccessor objectStoreAccessor)
    {
        _objectStoreAccessor = objectStoreAccessor;
        _options = new JsonSerializerOptions(JsonSerializerOptions.Strict)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { AttachProtocolEnvelopeConverters, AttachStoredObjectConverters }
            },
        };

        foreach (var converter in converters)
        {
            _options.Converters.Add((JsonConverter)converter);
        }
    }

    public JsonSerializerOptions GetOptions() => _options;

    private static void AttachProtocolEnvelopeConverters(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            var attribute = property.PropertyType
                .GetCustomAttributes(typeof(ProtocolEnvelopeAttribute), inherit: false)
                .Cast<ProtocolEnvelopeAttribute>()
                .FirstOrDefault();

            if (attribute is null)
            {
                continue;
            }

            var expectedName = attribute.Name ?? StripEnvelopeSuffix(property.PropertyType.Name);
            property.CustomConverter = (JsonConverter)Activator.CreateInstance(
                typeof(ProtocolEnvelopeConverter<>).MakeGenericType(property.PropertyType),
                expectedName)!;
        }
    }

    private void AttachStoredObjectConverters(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            var isStoredObject = property.AttributeProvider?
                .GetCustomAttributes(typeof(StoredObjectAttribute), inherit: false)
                .Length > 0;

            if (isStoredObject)
            {
                property.CustomConverter = (JsonConverter)Activator.CreateInstance(
                    typeof(StoredObjectConverter<>).MakeGenericType(property.PropertyType),
                    _objectStoreAccessor)!;
            }
        }
    }

    private static string StripEnvelopeSuffix(string name)
    {
        if (name.EndsWith("Request", StringComparison.Ordinal))
        {
            return name[..^"Request".Length];
        }

        return name.EndsWith("Response", StringComparison.Ordinal) ? name[..^"Response".Length] : name;
    }
}
