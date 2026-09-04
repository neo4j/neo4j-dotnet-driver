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

internal class TransactionRollbacker : ITransactionRollback
{
    private readonly IQueryApiHttpTransport _httpTransport;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly IQueryApiTransactionContextTracker _txContextTracker;

    public TransactionRollbacker(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpTransport httpTransport,
        IQueryApiTransactionContextTracker txContextTracker,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _httpTransport = httpTransport;
        _txContextTracker = txContextTracker;
        _logger = logger;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_txContextTracker.IsFailed)
        {
            _logger.LogDebug(
                "Transaction {txId} already failed server-side; skipping rollback request",
                _txContextTracker.Context!.TxId);

            return;
        }

        _logger.LogDebug("Rolling back transaction {txId}", _txContextTracker.Context!.TxId);
        using var request = await _requestBuilder.DeleteAsync($"query/v2/tx/{_txContextTracker.Context!.TxId}", cancellationToken).ConfigureAwait(false);
        using var response = await _httpTransport.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
