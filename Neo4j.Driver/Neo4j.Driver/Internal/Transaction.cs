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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;

namespace Neo4j.Driver.Internal
{
    internal class Transaction : StatementRunner, ITransaction, IBookmarkTracker
    {
        private readonly IConnection _connection;
        private readonly SyncExecutor _syncExecutor;
        private readonly IBoltProtocol _protocol;
        private ITransactionResourceHandler _resourceHandler;

        private Bookmark _bookmark;

        private State _state = State.Active;
        private IDriverLogger _logger;

        private enum State
        {
            /** The transaction is running with no explicit success or failure marked */
            Active,

            /** Running, user marked for success, meaning it'll value committed */
            MarkedSuccess,

            /** User marked as failed, meaning it'll be rolled back. */
            MarkedFailed,

            /**
             * An error has occurred, transaction can no longer be used and no more messages will be sent for this
             * transaction.
             */
            Failed,

            /** This transaction has successfully committed */
            Succeeded,

            /** This transaction has been rolled back */
            RolledBack
        }

        public Transaction(IConnection connection, SyncExecutor syncExecutor,
            ITransactionResourceHandler resourceHandler = null,
            IDriverLogger logger = null, Bookmark bookmark = null)
        {
            _connection = new TransactionConnection(this, connection);
            _syncExecutor = syncExecutor;
            _protocol = _connection.BoltProtocol;
            _resourceHandler = resourceHandler;
            _bookmark = bookmark;
            _logger = logger;
        }

        public void BeginTransaction(TransactionConfig txConfig)
        {
            _syncExecutor.RunSync(() => _protocol.BeginTransactionAsync(_connection, _bookmark, txConfig));
        }

        public Task BeginTransactionAsync(TransactionConfig txConfig)
        {
            return _protocol.BeginTransactionAsync(_connection, _bookmark, txConfig);
        }

        public override IStatementResult Run(Statement statement)
        {
            return TryExecute(_logger, () =>
            {
                EnsureCanRunMoreStatements();
                return new StatementResult(_syncExecutor.RunSync<IStatementResultCursor>(() =>
                    _protocol.RunInExplicitTransactionAsync(_connection, statement)), _syncExecutor);
            });
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            return TryExecuteAsync<IStatementResultCursor>(_logger, () =>
            {
                EnsureCanRunMoreStatements();
                return _protocol.RunInExplicitTransactionAsync(_connection, statement);
            });
        }

        public void Success()
        {
            if (_state == State.Active)
            {
                _state = State.MarkedSuccess;
            }
        }

        public void Failure()
        {
            if (_state == State.Active || _state == State.MarkedSuccess)
            {
                _state = State.MarkedFailed;
            }
        }

        public Task CommitAsync()
        {
            Success();
            return CloseAsync();
        }

        public Task RollbackAsync()
        {
            Failure();
            return CloseAsync();
        }

        public void MarkToClose()
        {
            _state = State.Failed;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            try
            {
                if (_state == State.MarkedSuccess)
                {
                    _syncExecutor.RunSync(CommitTxAsync);
                }
                else if (_state == State.MarkedFailed || _state == State.Active)
                {
                    _syncExecutor.RunSync(RollbackTxAsync);
                }
            }
            finally
            {
                _syncExecutor.RunSync(() => _connection.CloseAsync());
                if (_resourceHandler != null)
                {
                    _syncExecutor.RunSync(() => _resourceHandler.OnTransactionDisposeAsync(_bookmark));
                    _resourceHandler = null;
                }
            }
        }

        private async Task CloseAsync()
        {
            try
            {
                if (_state == State.MarkedSuccess)
                {
                    await CommitTxAsync().ConfigureAwait(false);
                }
                else if (_state == State.MarkedFailed || _state == State.Active)
                {
                    await RollbackTxAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await _connection.CloseAsync().ConfigureAwait(false);
                if (_resourceHandler != null)
                {
                    await _resourceHandler.OnTransactionDisposeAsync(_bookmark).ConfigureAwait(false);
                    _resourceHandler = null;
                }
            }
        }

        private async Task CommitTxAsync()
        {
            await _protocol.CommitTransactionAsync(_connection, this).ConfigureAwait(false);
            _state = State.Succeeded;
        }

        private async Task RollbackTxAsync()
        {
            await _protocol.RollbackTransactionAsync(_connection).ConfigureAwait(false);
            _state = State.RolledBack;
        }

        private void EnsureCanRunMoreStatements()
        {
            if (_state == State.RolledBack)
            {
                throw new ClientException(
                    "Cannot run more statements in this transaction, because previous statements in the " +
                    "transaction has failed and the transaction has been rolled back. Please start a new" +
                    " transaction to run another statement."
                );
            }

            if (_state == State.Succeeded)
            {
                throw new ClientException(
                    "Cannot run more statements in this transaction, because the transaction has already been committed successfully. " +
                    "Please start a new transaction to run another statement.");
            }

            if (_state == State.Failed || _state == State.MarkedFailed)
            {
                throw new ClientException(
                    "Cannot run more statements in this transaction, because previous statements in the " +
                    "transaction has failed and the transaction could only be rolled back. Please start a new" +
                    " transaction to run another statement."
                );
            }
        }

        public void UpdateBookmark(Bookmark bookmark)
        {
            _bookmark = bookmark;
        }

        private class TransactionConnection : DelegatedConnection
        {
            private Transaction _transaction;

            public TransactionConnection(Transaction transaction, IConnection connection)
                : base(connection)
            {
                _transaction = transaction;
            }

            public override Task CloseAsync()
            {
                // no resources will be closed as the resources passed in this class are managed outside this class
                Delegate = null;
                _transaction = null;
                return TaskHelper.GetCompletedTask();
            }

            public override Task OnErrorAsync(Exception error)
            {
                _transaction.MarkToClose();
                return TaskHelper.GetFailedTask(error);
            }
        }
    }
}