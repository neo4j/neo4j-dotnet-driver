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
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class RollbackTransactionHandler : IRollbackTransactionHandler
{
    private readonly IQueryApiHttpTransport _httpTransport;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly QueryApiTransactionContext _txContext;

    public RollbackTransactionHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpTransport httpTransport,
        QueryApiTransactionContext txContext,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _httpTransport = httpTransport;
        _txContext = txContext;
        _logger = logger;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Rolling back transaction {txId}", _txContext.TxId);
        using var request = await _requestBuilder.DeleteAsync($"query/v2/tx/{_txContext.TxId}", cancellationToken).ConfigureAwait(false);
        using var response = await _httpTransport.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
