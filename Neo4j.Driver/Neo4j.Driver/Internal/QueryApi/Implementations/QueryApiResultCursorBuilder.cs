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
using System.Linq;
using Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;
using Neo4j.Driver.Internal.Result;

using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiResultCursorBuilder : IQueryApiResultCursorBuilder
{
    private readonly IJsonValueConverter _jsonValueConverter;
    private readonly IResultSummaryFactory _summaryFactory;

    public QueryApiResultCursorBuilder(
        IResultSummaryFactory summaryFactory,
        IJsonValueConverter jsonValueConverter)
    {
        _summaryFactory = summaryFactory;
        _jsonValueConverter = jsonValueConverter;
    }

    public IResultCursor Build(QueryApiResultSet resultSet, Query query)
    {
        var lookup = new Dictionary<string, int>(resultSet.Fields.Length, StringComparer.Ordinal);
        var invariantLookup = new Dictionary<string, int>(resultSet.Fields.Length, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < resultSet.Fields.Length; i++)
        {
            lookup[resultSet.Fields[i]] = i;
            invariantLookup[resultSet.Fields[i]] = i;
        }

        var records = resultSet.Rows
            .Select(IRecord (row) => new Record(
                lookup,
                invariantLookup,
                row.Select(_jsonValueConverter.Convert).ToArray()!))
            .ToList();

        return new QueryApiResultCursor(records, resultSet.Fields, query, _summaryFactory);
    }
}
