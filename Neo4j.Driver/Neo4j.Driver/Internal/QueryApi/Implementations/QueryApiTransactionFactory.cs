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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using ILogger = Neo4j.Driver.Internal.QueryApi.Abstractions.ILogger;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiTransactionFactory : IQueryApiTransactionFactory
{
    private readonly IBeginTransactionHandler _beginTransactionHandler;
    private readonly ILogger _logger;
    private readonly IResolutionScope _resolutionScope;

    public QueryApiTransactionFactory(
        IBeginTransactionHandler beginTransactionHandler,
        IResolutionScope resolutionScope,
        ILogger logger)
    {
        _beginTransactionHandler = beginTransactionHandler;
        _resolutionScope = resolutionScope;
        _logger = logger;
    }

    public async Task<IInternalAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder>? action,
        IReadOnlyList<string> bookmarks,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Opening {mode} transaction", mode);
        var context = await _beginTransactionHandler
            .BeginTransactionAsync(bookmarks, cancellationToken)
            .ConfigureAwait(false);

        var txScope = _resolutionScope.CreateChildScope(r => r.RegisterInstance(context));

        return txScope.Resolve<IInternalAsyncTransaction>();
    }
}
