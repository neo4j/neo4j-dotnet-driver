// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal;

internal class AsyncTransaction : AsyncQueryRunner, IInternalAsyncTransaction, IBookmarksTracker
{
    private static readonly IState Active = new ActiveState();
    private static readonly IState Committed = new CommittedState();
    private static readonly IState RolledBack = new RolledBackState();
    private static readonly IState Failed = new FailedState();

    private readonly IConnection _connection;
    private readonly long _fetchSize;
    private readonly ILogger _logger;
    private readonly INotificationsConfig _notificationsConfig;
    private readonly bool _reactive;
    private readonly ITransactionResourceHandler _resourceHandler;

    private readonly IList<Task<IResultCursor>> _results = new List<Task<IResultCursor>>();
    private readonly SessionConfig _sessionConfig;

    private Bookmarks _bookmarks;

    private bool _disposed;
    private IState _state = Active;

    public AsyncTransaction(
        IConnection connection,
        ITransactionResourceHandler resourceHandler,
        ILogger logger = null,
        string database = null,
        Bookmarks bookmark = null,
        bool reactive = false,
        long fetchSize = Config.Infinite,
        SessionConfig sessionConfig = null,
        INotificationsConfig notificationsConfig = null)
    {
        _connection = new TransactionConnection(this, connection);
        _resourceHandler = resourceHandler ?? throw new ArgumentNullException(nameof(resourceHandler));
        _bookmarks = bookmark;
        _logger = logger;
        _reactive = reactive;
        Database = database;
        _fetchSize = fetchSize;
        _sessionConfig = sessionConfig;
        _notificationsConfig = notificationsConfig;
    }

    private string Database { get; set; }
    internal Exception TransactionError { get; set; }

    public void UpdateBookmarks(Bookmarks bookmarks, IDatabaseInfo dbInfo = null)
    {
        if (dbInfo != null && dbInfo.Name != Database)
        {
            Database = dbInfo.Name;
        }

        _bookmarks = bookmarks;
    }

    /// <summary>
    /// Sets the error for the transaction if it is not already set.
    /// This avoids the exception changing if multiple errors occur.
    /// </summary>
    /// <param name="ex">The first exception to occur in the transaction.</param>
    internal void SetErrorIfNull(Exception ex)
    {
        TransactionError ??= ex;
    }

    public bool IsErrored(out Exception ex)
    {
        if (TransactionError != null)
        {
            ex = TransactionError;
            return true;
        }

        ex = null;
        return false;
    }

    public bool IsOpen => _state == Active;

    public override Task<IResultCursor> RunAsync(Query query)
    {
        if (TransactionError != null)
        {
            throw new TransactionTerminatedException(TransactionError);
        }
        
        var result = _state.RunAsync(query, _connection, _logger, _reactive, _fetchSize, this,  out var nextState);
        _state = nextState;
        _results.Add(result);
        return result;
    }

    public async Task CommitAsync()
    {
        if (TransactionError != null)
        {
            throw new TransactionTerminatedException(TransactionError);
        }
        try
        {
            await DiscardUnconsumed().ConfigureAwait(false);
            await _state.CommitAsync(_connection, this, out var nextState).ConfigureAwait(false);
            _state = nextState;
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (TransactionError == null)
            {
                await DiscardUnconsumed().ConfigureAwait(false);
            }
            await _state.RollbackAsync(_connection, this, out var nextState).ConfigureAwait(false);
            _state = nextState;
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    public TransactionConfig TransactionConfig { get; private set; }

    public Task BeginTransactionAsync(TransactionConfig config, TransactionInfo transactionInfo)
    {
        TransactionConfig = config;
        
        return _connection.BeginTransactionAsync(
            new BeginTransactionParams(
                Database,
                _bookmarks,
                TransactionConfig,
                _sessionConfig,
                _notificationsConfig,
                transactionInfo));
    }

    public async Task MarkToCloseAsync()
    {
        _state = Failed;
        await DisposeTransactionAsync().ConfigureAwait(false);
    }

    private async Task DisposeTransactionAsync()
    {
        if (!Volatile.Read(ref _disposed))
        {
            await _resourceHandler.OnTransactionDisposeAsync(_bookmarks, Database).ConfigureAwait(false);
            Volatile.Write(ref _disposed, true);
        }
    }

    //Needed to implement the DisposeAsync interface correctly. This is called from the parent class that is
    //implementing the rest of the pattern.
    protected override async ValueTask DisposeAsyncCore()
    {
        if (IsOpen)
        {
            await RollbackAsync().ConfigureAwait(false);
        }
    }

    private async Task DiscardUnconsumed()
    {
        foreach (var result in _results)
        {
            IResultCursor cursor = null;
            try
            {
                cursor = await result.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignore if cursor failed to create
            }

            if (cursor != null)
            {
                await cursor.ConsumeAsync().ConfigureAwait(false);
            }
        }
    }

    private class TransactionConnection : DelegatedConnection
    {
        private AsyncTransaction _transaction;

        public TransactionConnection(AsyncTransaction transaction, IConnection connection)
            : base(connection)
        {
            _transaction = transaction;
        }

        public override Task CloseAsync()
        {
            // no resources will be closed as the resources passed in this class are managed outside this class
            Delegate = null;
            _transaction = null;
            return Task.CompletedTask;
        }

        internal override async Task OnErrorAsync(Exception error)
        {
            _transaction.SetErrorIfNull(error);
            await _transaction.MarkToCloseAsync().ConfigureAwait(false);
            throw error;
        }
    }

    private interface IState
    {
        Task<IResultCursor> RunAsync(
            Query query,
            IConnection connection,
            ILogger logger,
            bool reactive,
            long fetchSize,
            IInternalAsyncTransaction transaction,
            out IState nextState);

        Task CommitAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState);

        Task RollbackAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState);
    }

    private class ActiveState : IState
    {
        public Task<IResultCursor> RunAsync(
            Query query,
            IConnection connection,
            ILogger logger,
            bool reactive,
            long fetchSize,
            IInternalAsyncTransaction transaction,
            out IState nextState)
        {
            nextState = Active;
            return connection.RunInExplicitTransactionAsync(query, reactive, fetchSize, transaction);
        }

        public Task CommitAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            nextState = Committed;
            return connection.CommitTransactionAsync(tracker);
        }

        public Task RollbackAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            nextState = RolledBack;
            return connection.RollbackTransactionAsync();
        }
    }

    private class CommittedState : IState
    {
        public Task<IResultCursor> RunAsync(
            Query query,
            IConnection connection,
            ILogger logger,
            bool reactive,
            long fetchSize,
            IInternalAsyncTransaction transaction,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot run query in this transaction, because it has already been committed.");
        }

        public Task CommitAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot commit this transaction, because it has already been committed.");
        }

        public Task RollbackAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot rollback this transaction, because it has already been committed.");
        }
    }

    private class RolledBackState : IState
    {
        public Task<IResultCursor> RunAsync(
            Query query,
            IConnection connection,
            ILogger logger,
            bool reactive,
            long fetchSize,
            IInternalAsyncTransaction transaction,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot run query in this transaction, because it has already been rolled back.");
        }

        public Task CommitAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot commit this transaction, because it has already been rolled back.");
        }

        public Task RollbackAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot rollback this transaction, because it has already been rolled back.");
        }
    }

    private class FailedState : IState
    {
        public Task<IResultCursor> RunAsync(
            Query query,
            IConnection connection,
            ILogger logger,
            bool reactive,
            long fetchSize,
            IInternalAsyncTransaction transaction,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot run query in this transaction, because it has been rolled back either because of an error or explicit termination.");
        }

        public Task CommitAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            throw new TransactionClosedException(
                "Cannot commit this transaction, because it has been rolled back either because of an error or explicit termination.");
        }

        public Task RollbackAsync(
            IConnection connection,
            IBookmarksTracker tracker,
            out IState nextState)
        {
            nextState = Failed;
            return Task.CompletedTask;
        }
    }
}
