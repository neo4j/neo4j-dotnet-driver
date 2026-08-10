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
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Serialization;

internal class RegistryObjectConverterFactory : JsonConverterFactory, IProtocolJsonConverter
{
    private readonly IRegistry _registry;

    public RegistryObjectConverterFactory(IRegistry registry)
    {
        _registry = registry;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
            typeToConvert.GetGenericTypeDefinition() == typeof(RegistryObject<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var storedType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(RegistryObjectConverter<>).MakeGenericType(storedType), _registry)!;
    }
}

internal class RegistryObjectConverter<T> : JsonConverter<RegistryObject<T>> where T : notnull
{
    private readonly IRegistry _registry;

    public RegistryObjectConverter(IRegistry registry)
    {
        _registry = registry;
    }

    public override bool HandleNull => true;

    public override RegistryObject<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new TestKitProtocolException(
                $"A {typeof(T).Name} handle id must be a string, not {reader.TokenType}.");
        }

        return _registry.Get<T>(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, RegistryObject<T> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException("RegistryObject<T> is never serialized.");
    }
}
