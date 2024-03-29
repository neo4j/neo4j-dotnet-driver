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
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal interface IInternalAsyncSession : IAsyncSession
{
    Task<IAsyncTransaction> BeginTransactionAsync(
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult);

    Task<IAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult);

    Task<IResultCursor> RunAsync(
        Query query,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult);

    Task<EagerResult<T>> PipelinedExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<EagerResult<T>>> func, TransactionConfig config);
    Task<EagerResult<T>> PipelinedExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<EagerResult<T>>> func, TransactionConfig config);
}
