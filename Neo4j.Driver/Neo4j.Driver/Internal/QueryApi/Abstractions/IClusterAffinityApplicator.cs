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

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Applies the <c>neo4j-cluster-affinity</c> header to outgoing requests, and extracts it from incoming
/// responses, keeping the header name in one place.
/// </summary>
internal interface IClusterAffinityApplicator
{
    /// <summary>
    /// Adds the cluster-affinity header to <paramref name="request"/> when <paramref name="context"/> carries an
    /// affinity value.
    /// </summary>
    void Apply(HttpRequestMessage request, QueryApiTransactionContext context);

    /// <summary>
    /// Reads the cluster-affinity header value from <paramref name="response"/>, or returns <c>null</c> if the header
    /// is absent.
    /// </summary>
    string? Extract(HttpResponseMessage response);
}
