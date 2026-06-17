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

using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiJsonDeserializer : IJsonDeserializer
{
    private static readonly JsonNamingPolicy DefaultNamingPolicy = JsonNamingPolicy.CamelCase;

    private static readonly ConcurrentDictionary<JsonNamingPolicy, JsonSerializerOptions> OptionsByNamingPolicy = new()
    {
        [DefaultNamingPolicy] = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = DefaultNamingPolicy,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }
    };

    public ValueTask<T?> DeserializeAsync<T>(
        Stream utf8Json,
        JsonNamingPolicy? namingPolicy,
        CancellationToken cancellationToken = default)
    {
        var options = GetOptions(namingPolicy);
        return JsonSerializer.DeserializeAsync<T>(utf8Json, options, cancellationToken);
    }

    public T MapObject<T>(
        JsonElement json,
        JsonNamingPolicy? namingPolicy)
    {
        var options = GetOptions(namingPolicy);
        return json.Deserialize<T>(options)
            ?? throw new JsonException($"Failed to deserialize JSON element to type '{typeof(T)}'.");
    }

    private static JsonSerializerOptions GetOptions(JsonNamingPolicy? namingPolicy = null)
    {
        return OptionsByNamingPolicy.GetOrAdd(
            namingPolicy ?? DefaultNamingPolicy,
            policy => new JsonSerializerOptions()
            {
                PropertyNamingPolicy = policy,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
    }

    public ValueTask<T?> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        return ValueTask.FromResult(JsonSerializer.Deserialize<T>(json, options));
    }
}
