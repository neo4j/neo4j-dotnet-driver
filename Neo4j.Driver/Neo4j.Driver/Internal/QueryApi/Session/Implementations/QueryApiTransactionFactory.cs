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
    private readonly QueryApiTransactionContextHolder _contextHolder;
    private readonly ILogger _logger;
    private readonly IResolutionScope _resolutionScope;
    private readonly ITransactionBeginner _transactionStarter;

    public QueryApiTransactionFactory(
        ITransactionBeginner transactionStarter,
        QueryApiTransactionContextHolder contextHolder,
        IResolutionScope resolutionScope,
        ILogger logger)
    {
        _transactionStarter = transactionStarter;
        _contextHolder = contextHolder;
        _resolutionScope = resolutionScope;
        _logger = logger;
    }

    public async Task<IInternalAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder>? action,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Opening {mode} transaction", mode);
        await _transactionStarter.BeginAsync(cancellationToken).ConfigureAwait(false);

        var context = _contextHolder.Context
            ?? throw new InvalidOperationException("Transaction context was not set after begin.");

        var txScope = _resolutionScope.CreateChildScope(r => r
            .RegisterInstance(context)
            .RegisterType<IHttpRequestEnricher, QueryApiClusterAffinityEnricher>());

        return txScope.Resolve<IInternalAsyncTransaction>();
    }
}
