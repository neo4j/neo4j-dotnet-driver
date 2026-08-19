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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiListCodec : IQueryApiContainerCodec
{
    public IEnumerable<object?> GetChildValues(object value)
    {
        return ((IEnumerable)value).Cast<object?>();
    }

    public bool CanRead(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        return typeName == "List";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        ArgumentNullException.ThrowIfNull(recurse);

        return element
            .GetProperty("_value")
            .EnumerateArray()
            .Select(recurse.Decode)
            .ToList();
    }

    public bool CanWrite(object? value)
    {
        return TryGetEnumerable(value, out _);
    }

    private bool TryGetEnumerable(object? value, [NotNullWhen(true)] out IEnumerable? enumerable)
    {
        enumerable = value as IEnumerable;
        return enumerable is not null and not string and not IDictionary and not byte[];
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        if (!TryGetEnumerable(value, out var enumerable))
        {
            throw new ArgumentException("Value must be an enumerable.", nameof(value));
        }

        ArgumentNullException.ThrowIfNull(recurse);

        var array = new JsonArray();

        foreach (var item in enumerable)
        {
            array.Add(recurse.Encode(item));
        }

        return CreateTypedEnvelope("List", array);
    }
}
