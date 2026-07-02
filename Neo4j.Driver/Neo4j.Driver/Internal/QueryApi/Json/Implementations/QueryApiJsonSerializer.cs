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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiJsonSerializer : IQueryApiJsonSerializer, IJsonDeserializer
{
    private static readonly JsonNamingPolicy DefaultNamingPolicy = JsonNamingPolicy.CamelCase;

    private readonly IRequiredMediaVersionCalculator _versionCalculator;
    private readonly JsonSerializerOptions _writeOptions;

    private readonly ConcurrentDictionary<JsonNamingPolicy, JsonSerializerOptions> _readOptionsByNamingPolicy = new();

    public QueryApiJsonSerializer(
        IEnumerable<IQueryApiJsonConverter> converters,
        IRequiredMediaVersionCalculator versionCalculator)
    {
        _versionCalculator = versionCalculator;

        var converterList = converters.Select(c => c.GetConverter()).ToList();

        _writeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = DefaultNamingPolicy,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        foreach (var converter in converterList)
        {
            _writeOptions.Converters.Add(converter);
        }
    }

    public SerializedBody Serialize(IQueryApiRequestBody body)
    {
        var json = JsonSerializer.Serialize(body, body.GetType(), _writeOptions);
        var version = _versionCalculator.Calculate(body.GetParameterValues());
        return new SerializedBody(json, version);
    }

    public ValueTask<T?> DeserializeAsync<T>(
        Stream utf8Json,
        JsonNamingPolicy? namingPolicy,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<T>(utf8Json, GetReadOptions(namingPolicy), cancellationToken);
    }

    public T MapObject<T>(JsonElement json, JsonNamingPolicy? namingPolicy)
    {
        return json.Deserialize<T>(GetReadOptions(namingPolicy)) ??
            throw new JsonException($"Failed to deserialize JSON element to type '{typeof(T)}'.");
    }

    public ValueTask<T?> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(JsonSerializer.Deserialize<T>(json, GetReadOptions()));
    }

    private JsonSerializerOptions GetReadOptions(JsonNamingPolicy? namingPolicy = null)
    {
        return _readOptionsByNamingPolicy.GetOrAdd(
            namingPolicy ?? DefaultNamingPolicy,
            policy => new JsonSerializerOptions
            {
                PropertyNamingPolicy = policy,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
    }
}
