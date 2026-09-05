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

/// <summary>
/// The parsed result of a Query API statement execution.
/// <para>
/// <see cref="Rows"/> uses <see cref="JsonElement"/> to defer Neo4j-type conversion to the cursor layer. Elements
/// are safe to hold indefinitely — they are backed by memory owned by the deserialized object graph, not by a live stream.
/// </para>
/// </summary>
internal class QueryRunResponse
{
    public static readonly QueryRunResponse Empty = new();

    public string[] Fields { get; init; } = [];
    public JsonElement[][] Rows { get; init; } = [];
    public string[] Bookmarks { get; init; } = [];
}
