// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;

namespace Neo4j.Driver.Internal
{
    internal class AsyncTransaction : AsyncQueryRunner, IInternalAsyncTransaction, IBookmarksTracker
    {
        private static readonly IState Active = new ActiveState();
        private static readonly IState Committed = new CommittedState();
        private static readonly IState RolledBack = new RolledBackState();
        private static readonly IState Failed = new FailedState();

        private readonly IConnection _connection;
        private readonly IBoltProtocol _protocol;
        private readonly bool _reactive;
        private readonly ITransactionResourceHandler _resourceHandler;
		private readonly string _impersonatedUser = null;

        private Bookmarks _bookmarks;

        private bool _disposed = false;
        private IState _state = Active;
        private readonly ILogger _logger;
        private readonly long _fetchSize;

        private readonly IList<Task<IResultCursor>> _results = new List<Task<IResultCursor>>();

        public AsyncTransaction(IConnection connection, ITransactionResourceHandler resourceHandler,
            ILogger logger = null, string database = null, Bookmarks bookmark = null, bool reactive = false,
            long fetchSize = Config.Infinite, string impersonatedUser = null)
        {
            _connection = new TransactionConnection(this, connection);
            _protocol = _connection.BoltProtocol;
            _resourceHandler = resourceHandler ?? throw new ArgumentNullException(nameof(resourceHandler));
            _bookmarks = bookmark;
            _logger = logger;
            _reactive = reactive;
            Database = database;
            _fetchSize = fetchSize;
			_impersonatedUser = impersonatedUser;
        }

        public bool IsOpen => _state == Active;
        internal string Database { get; private set; }

        public Task BeginTransactionAsync(TransactionConfig config)
        {
            TransactionConfig = config;
            return _protocol.BeginTransactionAsync(_connection, Database, _bookmarks, config, _impersonatedUser);
        }

        public override Task<IResultCursor> RunAsync(Query query)
        {
            var result = _state.RunAsync(query, _connection, _protocol, _logger, _reactive, _fetchSize, out var nextState);
            _state = nextState;
            _results.Add(result);
            return result;
        }

        public async Task CommitAsync()
        {
            try
            {
                await DiscardUnconsumed().ConfigureAwait(false);
                await _state.CommitAsync(_connection, _protocol, this, out var nextState).ConfigureAwait(false);
                _state = nextState;
            }
            finally
            {
                await DisposeTransaction().ConfigureAwait(false);
            }
        }

        public async Task RollbackAsync()
        {
            try
            {
                await DiscardUnconsumed().ConfigureAwait(false);
                await _state.RollbackAsync(_connection, _protocol, this, out var nextState).ConfigureAwait(false);
                _state = nextState;
            }
            finally
            {
                await DisposeTransaction().ConfigureAwait(false);
            }
        }

        public TransactionConfig TransactionConfig { get; private set; }

        public async Task MarkToClose()
        {
            _state = Failed;
            await DisposeTransaction().ConfigureAwait(false);
        }

        public void UpdateBookmarks(Bookmarks bookmarks, IDatabaseInfo dbInfo = null)
        {
            if (dbInfo != null && dbInfo.Name != Database)
                Database = dbInfo.Name;
            _bookmarks = bookmarks;
        }

        private async Task DisposeTransaction()
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
				await RollbackAsync();
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

            public override async Task OnErrorAsync(Exception error)
            {
                await _transaction.MarkToClose().ConfigureAwait(false);
                throw error;
            }
        }

        private interface IState
        {
            Task<IResultCursor> RunAsync(Query query, IConnection connection, IBoltProtocol protocol,
                ILogger logger, bool reactive, long fetchSize, out IState nextState);

            Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState);

            Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState);
        }

        private class ActiveState : IState
        {
            public Task<IResultCursor> RunAsync(Query query, IConnection connection,
                IBoltProtocol protocol, ILogger logger, bool reactive, long fetchSize,
                out IState nextState)
            {
                nextState = Active;
                return protocol.RunInExplicitTransactionAsync(connection, query, reactive, fetchSize);
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                nextState = Committed;
                return protocol.CommitTransactionAsync(connection, tracker);
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                nextState = RolledBack;
                return protocol.RollbackTransactionAsync(connection);
            }
        }

        private class CommittedState : IState
        {
            public Task<IResultCursor> RunAsync(Query query, IConnection connection,
                IBoltProtocol protocol, ILogger logger, bool reactive,
                long fetchSize,
                out IState nextState)
            {
                throw new TransactionClosedException(
                    "Cannot run query in this transaction, because it has already been committed.");
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                throw new TransactionClosedException("Cannot commit this transaction, because it has already been committed.");
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                throw new TransactionClosedException("Cannot rollback this transaction, because it has already been committed.");
            }
        }

        private class RolledBackState : IState
        {
            public Task<IResultCursor> RunAsync(Query query, IConnection connection,
                IBoltProtocol protocol, ILogger logger, bool reactive,
                long fetchSize,
                out IState nextState)
            {
                throw new TransactionClosedException(
                    "Cannot run query in this transaction, because it has already been rolled back.");
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                throw new TransactionClosedException("Cannot commit this transaction, because it has already been rolled back.");
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                throw new TransactionClosedException("Cannot rollback this transaction, because it has already been rolled back.");
            }
        }

        private class FailedState : IState
        {
            public Task<IResultCursor> RunAsync(Query query, IConnection connection,
                IBoltProtocol protocol, ILogger logger, bool reactive,
                long fetchSize,
                out IState nextState)
            {
                throw new TransactionClosedException(
                    "Cannot run query in this transaction, because it has been rolled back either because of an error or explicit termination.");
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                throw new TransactionClosedException(
                    "Cannot commit this transaction, because it has been rolled back either because of an error or explicit termination.");
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarksTracker tracker,
                out IState nextState)
            {
                nextState = Failed;
                return Task.CompletedTask;
            }
        }
    }
}