// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests.Extensions;

public static class SessionExtensions
{
    public static async Task<IResultSummary> RunAndConsumeAsync(
        this IAsyncQueryRunner runner,
        string query,
        object parameters = null)
    {
        var cursor = await runner.RunAsync(query, parameters);
        var summary = await cursor.ConsumeAsync();
        return summary;
    }

    public static Task<IRecord> RunAndSingleAsync(this IAsyncQueryRunner runner, string query, object parameters)
    {
        return RunAndSingleAsync(runner, query, parameters, r => r);
    }

    public static async Task<T> RunAndSingleAsync<T>(
        this IAsyncQueryRunner runner,
        string query,
        object parameters,
        Func<IRecord, T> operation)
    {
        var cursor = await runner.RunAsync(query, parameters);
        var result = await cursor.SingleAsync(operation);
        return result;
    }
}
