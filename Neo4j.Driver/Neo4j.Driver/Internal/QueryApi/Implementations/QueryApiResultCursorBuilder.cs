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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Neo4j.Driver.Internal.QueryApi.JsonConverters;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiResultCursorBuilder : IQueryApiResultCursorBuilder
{
    private readonly IResultSummaryFactory _summaryFactory;
    private readonly IJsonObjectConverter _jsonObjectConverter;

    public QueryApiResultCursorBuilder(
        IResultSummaryFactory summaryFactory,
        IJsonObjectConverter jsonObjectConverter)
    {
        _summaryFactory = summaryFactory;
        _jsonObjectConverter = jsonObjectConverter;
    }

    public IResultCursor Build(QueryApiResponse response, Query query)
    {
        var lookup = new Dictionary<string, int>(response.Fields.Length, StringComparer.Ordinal);
        var invariantLookup = new Dictionary<string, int>(response.Fields.Length, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < response.Fields.Length; i++)
        {
            lookup[response.Fields[i]] = i;
            invariantLookup[response.Fields[i]] = i;
        }

        var records = response.Rows
            .Select(IRecord (row) => new Record(
                lookup,
                invariantLookup,
                row.Select(ConvertElement).ToArray()!))
            .ToList();

        return new QueryApiResultCursor(records, response.Fields, query, _summaryFactory);
    }

    private object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.Object => _jsonObjectConverter.Convert(element),
            _ => throw new ArgumentOutOfRangeException(
                nameof(element),
                element.ValueKind,
                "Unexpected JSON value kind.")
        };
    }

}
