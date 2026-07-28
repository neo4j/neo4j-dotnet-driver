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

namespace Neo4j.Driver.TestKitBackend.Protocol;

internal class JsonOptionsProvider : IJsonOptionsProvider
{
    private readonly JsonSerializerOptions _options;

    public JsonOptionsProvider(IProtocolJsonConverter[] converters)
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { BindHandlesToIdMembers } },
        };

        foreach (var converter in converters)
        {
            _options.Converters.Add((JsonConverter)converter);
        }
    }

    public JsonSerializerOptions GetOptions() => _options;

    // A RegistryObject<T> property Foo binds to wire member fooId. A naming policy can't do
    // this — it never sees the property type.
    private static void BindHandlesToIdMembers(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            var isRegistryObject = property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(RegistryObject<>);

            if (isRegistryObject)
            {
                property.Name += "Id";
            }
        }
    }
}
