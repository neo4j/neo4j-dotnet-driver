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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiTransactionFactory : IQueryApiTransactionFactory
{
    private readonly ILogger _logger;
    private readonly IResolutionScope _resolutionScope;

    public QueryApiTransactionFactory(
        IResolutionScope resolutionScope,
        ILogger logger)
    {
        _resolutionScope = resolutionScope;
        _logger = logger;
    }

    public async Task<IInternalAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder>? action,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Opening {mode} transaction", mode);

        var transactionScope = _resolutionScope.CreateChildScope(r => r
            .RegisterType<IQueryApiTransactionContextTracker, QueryApiTransactionContextTracker>(singleton: true));

        var transaction = transactionScope.Resolve<IScopedTransaction>();
        transaction.Disposed += GetTransactionDisposedHandler(transactionScope);

        try
        {
            await transaction.BeginAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transactionScope.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return transaction;
    }

    private static AsyncEventHandler GetTransactionDisposedHandler(IAsyncDisposable transactionScope)
    {
        return TransactionDisposed;

        async Task TransactionDisposed(object? o, EventArgs eventArgs)
        {
            await transactionScope.DisposeAsync().ConfigureAwait(false);
        }
    }
}
