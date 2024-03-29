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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal interface IInternalDriver : IDriver
{
    IInternalAsyncSession Session(Action<SessionConfigBuilder> action, bool reactive);

    Task<EagerResult<TResult>> ExecuteQueryAsync<TResult>(
        Query query,
        Func<IAsyncEnumerable<IRecord>, Task<TResult>> streamProcessor,
        QueryConfig config = null,
        CancellationToken cancellationToken = default);

    Task<ExecutionSummary> GetRowsAsync(
        Query query,
        QueryConfig config,
        Action<IRecord> streamProcessor,
        CancellationToken cancellationToken
    );
}
