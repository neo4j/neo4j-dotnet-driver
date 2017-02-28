// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class Transaction : StatementRunner, ITransaction
    {
        private readonly IConnection _connection;
        private readonly ITransactionResourceHandler _resourceHandler;

        internal const string BookmarkKey = "bookmark";
        internal string Bookmark { get; private set; }

        private const string Begin = "BEGIN";
        private const string Commit = "COMMIT";
        private const string Rollback = "ROLLBACK";

        private State _state = State.Active;

        public Transaction(IConnection connection, ITransactionResourceHandler resourceHandler=null, ILogger logger=null, string bookmark = null) : base(logger)
        {
            _connection = new TransactionConnection(this, connection);
            _resourceHandler = resourceHandler;

            IDictionary<string, object> paramters = new Dictionary<string, object>();
            if (bookmark != null)
            {
                paramters.Add(BookmarkKey, bookmark);
            }
            _connection.Run(Begin, paramters);
            if (bookmark != null)
            {
                _connection.Sync();
            }
        }

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
                    try
                    {
                        _connection.Run(Commit, null, new BookmarkCollector(s => Bookmark = s));
                        _connection.Sync();
                        _state = State.Succeeded;
                    }
                    catch (Exception)
                    {
                        // if we ever failed to commit, then we rollback the tx
                        try
                        {
                            RollBackTx();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        throw;
                    }
                }
                else if (_state == State.MarkedFailed || _state == State.Active)
                {
                    RollBackTx();
                }
            }
            finally
            {
                _connection.Dispose();
                _resourceHandler?.OnTransactionDispose();
                base.Dispose(true);
            }
        }

        private void RollBackTx()
        {
            _connection.Run(Rollback, null, null, false/*DiscardAll*/);
            _connection.Sync();
            _state = State.RolledBack;
        }

        public override IStatementResult Run(string statement, IDictionary<string, object> parameters=null)
        {
            return TryExecute(() =>
            {
                EnsureNotFailed();

                var resultBuilder = new ResultBuilder(statement, parameters, () => _connection.ReceiveOne(),
                    _connection.Server);
                _connection.Run(statement, parameters, resultBuilder);
                _connection.Send();
                return resultBuilder.PreBuild();
            });
        }

        private void EnsureNotFailed()
        {
            if (_state == State.Failed || _state == State.MarkedFailed || _state == State.RolledBack)
            {
                throw new ClientException(
                    "Cannot run more statements in this transaction, because previous statements in the " +
                    "transaction has failed and the transaction has been rolled back. Please start a new" +
                    " transaction to run another statement."
                );
            }
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

        public void MarkToClose()
        {
            _state = State.Failed;
        }

        private class TransactionConnection : DelegatedConnection
        {
            private Transaction _transaction;

            public TransactionConnection(Transaction transaction, IConnection connection)
                :base(connection)
            {
                _transaction = transaction;
            }

            public override void Dispose()
            {
                // no resouce will be closed as the resources passed in this class are managed outside this class
                Delegate = null;
                _transaction = null;
            }

            public override void OnError(Exception error)
            {
                if (Delegate.IsOpen)
                {
                    _transaction.Failure();
                }
                else
                {
                    _transaction.MarkToClose();
                }
                throw error;
            }
        }
    }
}