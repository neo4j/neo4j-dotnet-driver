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

using System.Net.Http.Headers;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// The result of a <see cref="IQueryApiClient.ExecuteAsync{TBody}"/> call.
/// <para>
/// <see cref="ResponseHeaders"/> is captured before the <see cref="System.Net.Http.HttpResponseMessage"/> is
/// disposed — header objects survive disposal and are safe to hold.
/// </para>
/// </summary>
internal record QueryApiResult<TBody>(TBody Body, HttpResponseHeaders ResponseHeaders)
    where TBody : QueryApiResponse;
