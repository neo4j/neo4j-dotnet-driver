// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class Transaction : StatementRunner, ITransaction
    {
        private readonly TransactionConnection _connection;
        private readonly IBoltProtocol _protocol;
        private ITransactionResourceHandler _resourceHandler;

        internal Bookmark Bookmark { get; private set; }

        private State _state = State.Active;

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

        public Transaction(IConnection connection, ITransactionResourceHandler resourceHandler=null, ILogger logger=null, Bookmark bookmark = null) : base(logger)
        {
            _connection = new TransactionConnection(this, connection);
            _protocol = _connection.BoltProtocol;
            _resourceHandler = resourceHandler;
            Bookmark = bookmark;
        }

        public void BeginTransaction()
        {
            _protocol.BeginTransaction(_connection, Bookmark);
        }

        public Task BeginTransactionAsync()
        {
            return _protocol.BeginTransactionAsync(_connection, Bookmark);
        }

        public override IStatementResult Run(Statement statement)
        {
            return TryExecute(() =>
            {
                EnsureCanRunMoreStatements();
                return _protocol.RunInExplicitTransaction(_connection, statement);
            });
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            return TryExecuteAsync(() =>
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
                    CommitTx();
                }
                else if (_state == State.MarkedFailed || _state == State.Active)
                {
                    RollbackTx();
                }
            }
            finally
            {
                _connection.Close();
                if (_resourceHandler != null)
                {
                    _resourceHandler.OnTransactionDispose();
                    _resourceHandler = null;
                }
                base.Dispose(true);
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
                    await _resourceHandler.OnTransactionDisposeAsync().ConfigureAwait(false);
                    _resourceHandler = null;
                }
            }
        }

        private void CommitTx()
        {
            Bookmark = _protocol.CommitTransaction(_connection);
            _state = State.Succeeded;
        }

        private async Task CommitTxAsync()
        {
            Bookmark = await _protocol.CommitTransactionAsync(_connection).ConfigureAwait(false);
            _state = State.Succeeded;
        }

        private void RollbackTx()
        {
            _protocol.RollbackTransaction(_connection);
            _state = State.RolledBack;
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
            else if (_state == State.Succeeded)
            {
                throw new ClientException("Cannot run more sattements in this transaction, because the transaction has already been committed successfuly. " +
                                          "Please start a new transaction to run another statement.");
            }
            else if (_state == State.Failed || _state == State.MarkedFailed)
            {
                throw new ClientException(
                    "Cannot run more statements in this transaction, because previous statements in the " +
                    "transaction has failed and the transaction could only be rolled back. Please start a new" +
                    " transaction to run another statement."
                );
            }
        }

        private class TransactionConnection : DelegatedConnection
        {
            private Transaction _transaction;

            public TransactionConnection(Transaction transaction, IConnection connection)
                :base(connection)
            {
                _transaction = transaction;
            }

            public override void Close()
            {
                // no resources will be closed as the resources passed in this class are managed outside this class
                Delegate = null;
                _transaction = null;
            }

            public override Task CloseAsync()
            {
                Close();
                return TaskHelper.GetCompletedTask();
            }

            public override void OnError(Exception error)
            {
                _transaction.MarkToClose();
                throw error;
            }
        }
    }
}
