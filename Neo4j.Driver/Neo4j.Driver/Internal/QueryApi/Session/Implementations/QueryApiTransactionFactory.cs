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

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiTransactionFactory : IQueryApiTransactionFactory
{
    private readonly IAuthTokenManager _authTokenManager;
    private readonly IBookmarkTracker _bookmarkTracker;
    private readonly DriverContext _driverContext;
    private readonly IQueryApiHttpTransport _httpTransport;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServerInfo _serverInfo;
    private readonly ISessionContext _sessionContext;
    private readonly ILoggingContextTracker _sessionTracker;

    public QueryApiTransactionFactory(
        DriverContext driverContext,
        ISessionContext sessionContext,
        IAuthTokenManager authTokenManager,
        ILoggerFactory loggerFactory,
        ILoggingContextTracker sessionTracker,
        IQueryApiHttpTransport httpTransport,
        IServerInfo serverInfo,
        IBookmarkTracker bookmarkTracker,
        ILogger logger)
    {
        _driverContext = driverContext;
        _sessionContext = sessionContext;
        _authTokenManager = authTokenManager;
        _loggerFactory = loggerFactory;
        _sessionTracker = sessionTracker;
        _httpTransport = httpTransport;
        _serverInfo = serverInfo;
        _bookmarkTracker = bookmarkTracker;
        _logger = logger;
    }

    public async Task<IInternalAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder>? action,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Opening {mode} transaction", mode);

        var composition = new QueryApiTransactionComposition(
            _driverContext,
            _sessionContext,
            _authTokenManager,
            _loggerFactory,
            _sessionTracker,
            _httpTransport,
            _serverInfo,
            _bookmarkTracker);

        var transaction = composition.Transaction();
        transaction.Disposed += GetTransactionDisposedHandler(composition);

        try
        {
            await transaction.BeginAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            composition.Dispose();
            throw;
        }

        return transaction;
    }

    private static AsyncEventHandler GetTransactionDisposedHandler(IDisposable composition)
    {
        return TransactionDisposed;

        Task TransactionDisposed(object? o, EventArgs eventArgs)
        {
            composition.Dispose();
            return Task.CompletedTask;
        }
    }
}
