// Copyright (c) 2002-2019 "Neo4j,"
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
    internal class AsyncTransaction : AsyncStatementRunner, IInternalAsyncTransaction, IBookmarkTracker
    {
        private static readonly IState Active = new ActiveState();
        private static readonly IState Committed = new CommittedState();
        private static readonly IState RolledBack = new RolledBackState();
        private static readonly IState Failed = new FailedState();

        private readonly IConnection _connection;
        private readonly IBoltProtocol _protocol;
        private readonly bool _reactive;
        private readonly ITransactionResourceHandler _resourceHandler;
        private readonly string _database;

        private Bookmark _bookmark;

        private bool _disposed = false;
        private IState _state = Active;
        private readonly IDriverLogger _logger;
        private readonly long _fetchSize;

        private readonly IList<Task<IStatementResultCursor>> _results = new List<Task<IStatementResultCursor>>();

        public AsyncTransaction(IConnection connection, ITransactionResourceHandler resourceHandler,
            IDriverLogger logger = null, string database = null, Bookmark bookmark = null, bool reactive = false,
            long fetchSize = Config.Infinite)
        {
            _connection = new TransactionConnection(this, connection);
            _protocol = _connection.BoltProtocol;
            _resourceHandler = resourceHandler ?? throw new ArgumentNullException(nameof(resourceHandler));
            _bookmark = bookmark;
            _logger = logger;
            _reactive = reactive;
            _database = database;
            _fetchSize = fetchSize;
        }

        public bool IsOpen => _state == Active;

        public Task BeginTransactionAsync(TransactionOptions optionsBuilder)
        {
            return _protocol.BeginTransactionAsync(_connection, _database, _bookmark, optionsBuilder);
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            var result = _state.RunAsync(statement, _connection, _protocol, _logger, _reactive, _fetchSize, out var nextState);
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
                await _state.RollbackAsync(_connection, _protocol, this, out var nextState);
                _state = nextState;
            }
            finally
            {
                await DisposeTransaction().ConfigureAwait(false);
            }
        }

        public async Task MarkToClose()
        {
            _state = Failed;
            await DisposeTransaction().ConfigureAwait(false);
        }

        public void UpdateBookmark(Bookmark bookmark)
        {
            _bookmark = bookmark;
        }

        private async Task DisposeTransaction()
        {
            if (!Volatile.Read(ref _disposed))
            {
                await _resourceHandler.OnTransactionDisposeAsync(_bookmark).ConfigureAwait(false);
                Volatile.Write(ref _disposed, true);
            }
        }

        private async Task DiscardUnconsumed()
        {
            foreach (var result in _results)
            {
                IStatementResultCursor cursor = null;
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
                    await cursor.SummaryAsync().ConfigureAwait(false);
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
                await _transaction.MarkToClose();
                throw error;
            }
        }

        private interface IState
        {
            Task<IStatementResultCursor> RunAsync(Statement statement, IConnection connection, IBoltProtocol protocol,
                IDriverLogger logger, bool reactive, long fetchSize, out IState nextState);

            Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState);

            Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState);
        }

        private class ActiveState : IState
        {
            public Task<IStatementResultCursor> RunAsync(Statement statement, IConnection connection,
                IBoltProtocol protocol, IDriverLogger logger, bool reactive, long fetchSize,
                out IState nextState)
            {
                nextState = Active;
                return protocol.RunInExplicitTransactionAsync(connection, statement, reactive, fetchSize);
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                nextState = Committed;
                return protocol.CommitTransactionAsync(connection, tracker);
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                nextState = RolledBack;
                return protocol.RollbackTransactionAsync(connection);
            }
        }

        private class CommittedState : IState
        {
            public Task<IStatementResultCursor> RunAsync(Statement statement, IConnection connection,
                IBoltProtocol protocol, IDriverLogger logger, bool reactive,
                long fetchSize,
                out IState nextState)
            {
                throw new ClientException(
                    "Cannot run statement in this transaction, because it has already been committed.");
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                throw new ClientException("Cannot commit this transaction, because it has already been committed.");
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                throw new ClientException("Cannot rollback this transaction, because it has already been committed.");
            }
        }

        private class RolledBackState : IState
        {
            public Task<IStatementResultCursor> RunAsync(Statement statement, IConnection connection,
                IBoltProtocol protocol, IDriverLogger logger, bool reactive,
                long fetchSize,
                out IState nextState)
            {
                throw new ClientException(
                    "Cannot run statement in this transaction, because it has already been rolled back.");
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                throw new ClientException("Cannot commit this transaction, because it has already been rolled back.");
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                throw new ClientException("Cannot rollback this transaction, because it has already been rolled back.");
            }
        }

        private class FailedState : IState
        {
            public Task<IStatementResultCursor> RunAsync(Statement statement, IConnection connection,
                IBoltProtocol protocol, IDriverLogger logger, bool reactive,
                long fetchSize,
                out IState nextState)
            {
                throw new ClientException(
                    "Cannot run statement in this transaction, because it has been rolled back either because of an error or explicit termination.");
            }

            public Task CommitAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                throw new ClientException(
                    "Cannot commit this transaction, because it has been rolled back either because of an error or explicit termination.");
            }

            public Task RollbackAsync(IConnection connection, IBoltProtocol protocol, IBookmarkTracker tracker,
                out IState nextState)
            {
                nextState = Failed;
                return Task.CompletedTask;
            }
        }
    }
}