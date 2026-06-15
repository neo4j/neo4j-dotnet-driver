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

using System.Text.Json;
using System.Text.Json.Serialization;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Serializes objects to JSON using the HTTP Query API v1.0 typed format for parameter values
/// (<c>{"$type":"...", "_value":...}</c>), via <see cref="QueryApiParameterConverter"/>.
/// </summary>
[AutoRegister]
internal sealed class TypedJsonObjectSerializer : IJsonObjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new QueryApiParameterConverter() }
    };

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
