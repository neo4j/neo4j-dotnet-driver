// Copyright (c) 2002-2016 "Neo Technology,"
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
    internal class Session : StatementRunner, ISession
    {
        private readonly IConnection _connection;

        /* 
         * All operations that modify transation status or 
         * perform a certain action depending on the current transaction status 
         * should be syncronized,
         * as both reset thread and running thread could modify this filed at the same time.
         */
        private Transaction _transaction;
        private readonly object _txSyncLock = new object();

        private readonly Action _transactionCleanupAction;

        private readonly ILogger _logger;
        private bool _isOpen = true;

        public Session(IConnection conn, ILogger logger):base(logger)
        {
            _connection = conn;
            _transactionCleanupAction = () =>
            {
                LastBookmark = _transaction?.Bookmark;
                _transaction = null;
            };
            _logger = logger;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            TryExecute(() =>
            {
                if (_isOpen)
                {
                    // This will not protect the session being disposed concurrently
                    // a.k.a. Session is not thread-safe!
                    _isOpen = false;
                }
                else
                {
                    throw new InvalidOperationException("Failed to dispose this seesion as it has already been disposed.");
                }
                if (!_connection.IsOpen)
                {
                    // can not sync any data on this connection
                    _connection.Dispose();
                }
                else
                {
                    lock (_txSyncLock)
                    {
                        if (_transaction != null)
                        {
                            try
                            {
                                _transaction.Dispose();
                            }
                            catch
                            {
                                // Best-effort
                            }
                        }
                        try
                        {
                            _connection.Sync();
                        }
                        finally
                        {
                            _connection.Dispose();
                        }
                    }
                }
                
            });
            base.Dispose(true);
        }

        public override IStatementResult Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            return TryExecute(() =>
            {
                var resultBuilder = new ResultBuilder(statement, statementParameters, () => _connection.ReceiveOne(), _connection.Server);
                lock (_txSyncLock)
                {
                    EnsureCanRunMoreStatements();
                    _connection.Run(statement, statementParameters, resultBuilder);
                    _connection.Send();
                }
                return resultBuilder.PreBuild();
            });
        }

        public ITransaction BeginTransaction(string bookmark = null)
        {
            return TryExecute(() =>
            {
                lock (_txSyncLock)
                {
                    EnsureCanRunMoreStatements();
                    _transaction = new Transaction(_connection, _transactionCleanupAction, _logger, bookmark);
                }
                return _transaction;
            });
        }

        public string LastBookmark { get; private set; }

        private void EnsureCanRunMoreStatements()
        {
            EnsureConnectionIsHealthy();
            EnsureNoOpenTransaction();
            EnsureSessionIsOpen();
        }

        private void EnsureSessionIsOpen()
        {
            if (!_isOpen)
            {
                throw new ClientException("Cannot running more statements in the current session as it has already been disposed." +
                                          "Make sure that you do not have a bad reference to a disposed session " +
                                          "and retry your statement in another new session.");
            }
        }

        private void EnsureConnectionIsHealthy()
        {
            if (!_connection.IsOpen)
            {
                throw new ClientException("The current session cannot be reused as the underlying connection with the " +
                                           "server has been closed or is going to be closed due to unrecoverable errors. " +
                                           "Please close this session and retry your statement in another new session.");
            }
        }

        private void EnsureNoOpenTransaction()
        {
            if (_transaction != null)
            {
                throw new ClientException("Please close the currently open transaction object before running " +
                                           "more statements/transactions in the current session.");
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public void Reset()
        {
            EnsureSessionIsOpen();
            EnsureConnectionIsHealthy();

            lock (_txSyncLock)
            {
                _transaction?.MarkToClose();
                _transactionCleanupAction.Invoke();
            }
            _connection.ResetAsync();
        }
    }
}