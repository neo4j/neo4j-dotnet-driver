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

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>Appears in error arrays on both auto-commit and explicit-transaction responses.</summary>
internal record QueryApiErrorBody(string Code = "", string Message = "");

/// <summary>
/// The data envelope returned by auto-commit and run-in-transaction responses.
/// <c>Fields</c> defaults to an empty array so callers never receive null.
/// </summary>
internal record QueryApiDataBody
{
    public string[] Fields { get; init; } = [];
    public JsonElement[][]? Values { get; init; }
}

/// <summary>
/// The full response body shape shared by auto-commit and run-in-transaction endpoints:
/// <c>data.fields</c>, <c>data.values</c>, <c>bookmarks</c>, and an optional <c>errors</c> array.
/// </summary>
internal record QueryApiResultBody
{
    public QueryApiDataBody? Data { get; init; }
    public string[]? Bookmarks { get; init; }
    public QueryApiErrorBody[]? Errors { get; init; }
}
