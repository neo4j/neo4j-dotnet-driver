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

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>Validates HTTP-level and application-level errors from the Query API.</summary>
internal interface IQueryApiErrorChecker
{
    /// <summary>
    /// Throws if the response status is not <c>202 Accepted</c>. Parses the response body on <c>401</c> to surface
    /// the specific error code.
    /// </summary>
    Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws the appropriate <see cref="Neo4jException"/> for the first error in the array, if any.
    /// No-ops when <paramref name="errors"/> is null or empty.
    /// </summary>
    void ThrowIfErrors(QueryApiErrorBody[]? errors);
}
