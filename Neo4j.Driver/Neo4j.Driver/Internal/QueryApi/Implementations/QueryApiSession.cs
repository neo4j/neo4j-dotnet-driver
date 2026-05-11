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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiSession : IInternalAsyncSession
{
    private readonly IAutoCommitHandler _autoCommitHandler;
    private readonly IQueryApiResultCursorBuilder _cursorBuilder;
    private readonly IQueryApiTransactionFactory _transactionFactory;

    public QueryApiSession(
        SessionConfig sessionConfig,
        IAutoCommitHandler autoCommitHandler,
        IQueryApiResultCursorBuilder cursorBuilder,
        IQueryApiTransactionFactory transactionFactory)
    {
        SessionConfig = sessionConfig;
        _autoCommitHandler = autoCommitHandler;
        _cursorBuilder = cursorBuilder;
        _transactionFactory = transactionFactory;
    }

    public Bookmarks LastBookmarks { get; private set; } = Bookmarks.Empty;

    public SessionConfig SessionConfig { get; }

    // --- RunAsync -------------------------------------------------------

    public async Task<IResultCursor> RunAsync(
        Query query,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        var response = await _autoCommitHandler.AutoCommitAsync(query, LastBookmarks.Values);
        LastBookmarks = Bookmarks.From(response.Bookmarks);
        return _cursorBuilder.Build(response, query);
    }

    public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action) =>
        RunAsync(query, action, false);

    public Task<IResultCursor> RunAsync(Query query) =>
        RunAsync(query, null!);

    public Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action) =>
        RunAsync(new Query(query), action);

    public Task<IResultCursor> RunAsync(
        string query,
        IDictionary<string, object> parameters,
        Action<TransactionConfigBuilder> action) =>
        RunAsync(new Query(query, parameters), action);

    public Task<IResultCursor> RunAsync(string query) =>
        RunAsync(new Query(query));

    public Task<IResultCursor> RunAsync(string query, object parameters) =>
        RunAsync(new Query(query, parameters));

    public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters) =>
        RunAsync(new Query(query, parameters));

    // --- BeginTransactionAsync ------------------------------------------

    public Task<IAsyncTransaction> BeginTransactionAsync(
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult) =>
        BeginTransactionAsync(AccessMode.Write, action, disposeUnconsumedSessionResult);

    public Task<IAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult) =>
        _transactionFactory.BeginTransactionAsync(mode, action, LastBookmarks.Values);

    public Task<IAsyncTransaction> BeginTransactionAsync() =>
        BeginTransactionAsync(null!);

    public Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action) =>
        BeginTransactionAsync(action, false);

    public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode) =>
        BeginTransactionAsync(mode, null!);

    public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode, Action<TransactionConfigBuilder> action) =>
        BeginTransactionAsync(mode, action, false);

    // --- ExecuteRead/Write ----------------------------------------------

    public Task<TResult> ExecuteReadAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder>? action = null) =>
        Task.FromResult(default(TResult)!);

    public Task ExecuteReadAsync(
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder>? action = null) =>
        Task.CompletedTask;

    public Task<TResult> ExecuteWriteAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder>? action = null) =>
        Task.FromResult(default(TResult)!);

    public Task ExecuteWriteAsync(
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder>? action = null) =>
        Task.CompletedTask;

    // --- Pipelined ------------------------------------------------------

    public Task<EagerResult<T>> PipelinedExecuteReadAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config) =>
        Task.FromResult(default(EagerResult<T>)!);

    public Task<EagerResult<T>> PipelinedExecuteWriteAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config) =>
        Task.FromResult(default(EagerResult<T>)!);

    // --- Lifecycle ------------------------------------------------------

    public Task CloseAsync() => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() { }
}
