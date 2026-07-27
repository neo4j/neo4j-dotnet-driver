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
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Protocol;

// IOptional<T> is open-generic, so a factory closes OptionalConverter<T> per value type.
internal class OptionalConverterFactory : JsonConverterFactory, IProtocolJsonConverter
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
            typeToConvert.GetGenericTypeDefinition() == typeof(IOptional<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(OptionalConverter<>).MakeGenericType(valueType))!;
    }
}

internal class OptionalConverter<T> : JsonConverter<IOptional<T>>
{
    // A present JSON null must become Specified(null), distinct from an absent key. Without this,
    // STJ short-circuits null to a bare null value and never invokes Read.
    public override bool HandleNull => true;

    public override IOptional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The converter is only invoked when the key is present; absence is handled by the
        // message record defaulting the property to Missing. So present ⇒ Specified.
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional.Specified<T>(value!);
    }

    public override void Write(Utf8JsonWriter writer, IOptional<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
