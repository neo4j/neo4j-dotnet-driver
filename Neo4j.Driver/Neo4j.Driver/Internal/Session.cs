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
    internal class Session : StatementRunner, ISession
    {
        // If the connection is ever successfully created, 
        // then it is session's responsibility to dispose them properly
        // without any possible connection leak.
        private readonly Func<IConnection> _acquireConnFunc;
        private IConnection _connection;

        private IStatementResult _sessionRunResult;

        private Transaction _transaction;
        private readonly Action _transactionCleanupAction;

        private readonly ILogger _logger;
        private bool _isOpen = true;

        public Session(IConnection conn, ILogger logger=null) : this(() => conn, logger)
        {
            // If this connection is not used in run or beginTx, then it might not be disposed by session
        }

        public Session(Func<IConnection> acquireConnFunc, ILogger logger):base(logger)
        {
            _acquireConnFunc = acquireConnFunc;

            _transactionCleanupAction = () =>
            {
                LastBookmark = _transaction?.Bookmark;
                _transaction = null;
            };
            _logger = logger;
        }

        public override IStatementResult Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            return TryExecute(() =>
            {
                EnsureCanRunMoreStatements();

                _connection = _acquireConnFunc.Invoke();
                var resultBuilder = new ResultBuilder(statement, statementParameters, 
                    () => _connection.ReceiveOne(), _connection.Server);
                
                _connection.Run(statement, statementParameters, resultBuilder);
                _connection.Send();
                _sessionRunResult = resultBuilder.PreBuild();
                return _sessionRunResult;
            });
        }

        public ITransaction BeginTransaction(string bookmark = null)
        {
            return TryExecute(() =>
            {
                EnsureCanRunMoreStatements();

                _connection = _acquireConnFunc.Invoke();
                _transaction = new Transaction(_connection, _transactionCleanupAction, _logger, bookmark);
                return _transaction;
            });
        }

        public string LastBookmark { get; private set; }

        public Guid Id { get; } = Guid.NewGuid();

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
                    throw new ObjectDisposedException(GetType().Name,"Failed to dispose this seesion as it has already been disposed.");
                }

                DisposeOpenConnection();
            });
            base.Dispose(true);
        }

        private void EnsureCanRunMoreStatements()
        {
            EnsureSessionIsOpen();
            // Enusre dispose open connection will also try to close any existing open transaction,
            // So the check of no open transaction should always be in front of dispose open connection.
            EnsureNoOpenTransaction();
            DisposeOpenConnection();
        }

        private void DisposeOpenConnection()
        {
            // clean any session.run result reference.
            if (_sessionRunResult != null)
            {
                LastBookmark = null;
                _sessionRunResult = null;
            }

            // always close connection if connection is not null
            if (_connection != null)
            {
                try
                {
                    if (_connection.IsOpen)
                    {
                        DisposeTransaction();
                        _connection.Sync(); // this will pull all unread records into buffer
                    }
                }
                finally
                {
                    _connection.Dispose();
                    _connection = null;
                }
            }
        }

        private void DisposeTransaction()
        {
            // When there is a open transation, this method will aslo try to close the tx
            if (_transaction != null)
            {
                try
                {
                    _transaction.Dispose();
                }
                catch (Exception e)
                {
                    // only log the error but not throw
                    _logger.Error($"Failed to dispose transaction due to error: {e.Message}", e);
                }
                finally
                {
                    _transaction = null;
                }
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

        private void EnsureSessionIsOpen()
        {
            if (!_isOpen)
            {
                throw new ClientException("Cannot running more statements in the current session as it has already been disposed." +
                                          "Make sure that you do not have a bad reference to a disposed session " +
                                          "and retry your statement in another new session.");
            }
        }
    }
}