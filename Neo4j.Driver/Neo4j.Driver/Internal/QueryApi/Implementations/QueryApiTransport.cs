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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal sealed class QueryApiTransport : IQueryApiTransport
{
    private readonly IAutoCommitHandler _autoCommit;
    private readonly IBeginTransactionHandler _beginTransaction;
    private readonly ICommitTransactionHandler _commitTransaction;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IRollbackTransactionHandler _rollbackTransaction;
    private readonly IRunInTransactionHandler _runInTransaction;
    private readonly IVerifyConnectivityHandler _verifyConnectivity;

    public QueryApiTransport(Uri baseUri, Func<HttpMessageHandler>? handlerFactory = null)
    {
        var urlBuilder = new QueryApiUrlBuilder(baseUri);
        var httpClient = new QueryApiHttpClient(handlerFactory);
        var json = new QueryApiJsonSerializer();
        var errorChecker = new QueryApiErrorChecker(json);
        var authApplicator = new QueryApiAuthApplicator();
        var clusterAffinityApplicator = new QueryApiClusterAffinityApplicator();
        var requestBuilder = new QueryApiRequestBuilder(urlBuilder, authApplicator, clusterAffinityApplicator);

        _httpClient = httpClient;
        _autoCommit = new AutoCommitHandler(requestBuilder, httpClient, errorChecker, json, json);
        _beginTransaction = new BeginTransactionHandler(
            requestBuilder,
            httpClient,
            errorChecker,
            json,
            json,
            clusterAffinityApplicator);

        _runInTransaction = new RunInTransactionHandler(requestBuilder, httpClient, errorChecker, json, json);
        _commitTransaction = new CommitTransactionHandler(requestBuilder, httpClient, errorChecker, json);
        _rollbackTransaction = new RollbackTransactionHandler(requestBuilder, httpClient, errorChecker);
        _verifyConnectivity = new VerifyConnectivityHandler(requestBuilder, httpClient, errorChecker, json);
    }

    internal QueryApiTransport(
        IQueryApiHttpClient httpClient,
        IAutoCommitHandler autoCommit,
        IBeginTransactionHandler beginTransaction,
        IRunInTransactionHandler runInTransaction,
        ICommitTransactionHandler commitTransaction,
        IRollbackTransactionHandler rollbackTransaction,
        IVerifyConnectivityHandler verifyConnectivity)
    {
        _httpClient = httpClient;
        _autoCommit = autoCommit;
        _beginTransaction = beginTransaction;
        _runInTransaction = runInTransaction;
        _commitTransaction = commitTransaction;
        _rollbackTransaction = rollbackTransaction;
        _verifyConnectivity = verifyConnectivity;
    }

    public Task<QueryApiResponse> AutoCommitAsync(
        string database,
        Query query,
        IReadOnlyList<string> bookmarks,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        return _autoCommit.AutoCommitAsync(database, query, bookmarks, auth, cancellationToken);
    }

    public Task<QueryApiTransactionContext> BeginTransactionAsync(
        string database,
        IReadOnlyList<string> bookmarks,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        return _beginTransaction.BeginTransactionAsync(database, bookmarks, auth, cancellationToken);
    }

    public Task<QueryApiResponse> RunInTransactionAsync(
        string database,
        QueryApiTransactionContext txContext,
        Query query,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        return _runInTransaction.RunInTransactionAsync(database, txContext, query, auth, cancellationToken);
    }

    public Task<string[]> CommitTransactionAsync(
        string database,
        QueryApiTransactionContext txContext,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        return _commitTransaction.CommitTransactionAsync(database, txContext, auth, cancellationToken);
    }

    public Task RollbackTransactionAsync(
        string database,
        QueryApiTransactionContext txContext,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        return _rollbackTransaction.RollbackTransactionAsync(database, txContext, auth, cancellationToken);
    }

    public Task<IServerInfo> VerifyConnectivityAsync(
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        return _verifyConnectivity.VerifyConnectivityAsync(auth, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _httpClient.DisposeAsync();
    }
}
