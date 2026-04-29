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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal interface IVerifyConnectivityHandler
{
    /// <summary>
    /// Verifies the server is reachable and returns basic server information.
    /// Uses the <c>system</c> database with <c>RETURN 1</c> — the precise semantics
    /// of a connectivity check over the Query API are an open question.
    /// </summary>
    Task<IServerInfo> VerifyConnectivityAsync(
        IAuthToken auth,
        CancellationToken cancellationToken = default);
}
