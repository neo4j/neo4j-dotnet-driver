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
        private Transaction _transaction;
        private readonly Action _transactionCleanupAction;

        private readonly ILogger _logger;
        private bool _isOpen = true;

        public Session(IConnection conn, ILogger logger):base(logger)
        {
            _connection = conn;
            _transactionCleanupAction = () => { _transaction = null; };
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
                _connection.Dispose();
            });

            base.Dispose(true);
        }

        public override IStatementResult Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            return TryExecute(() =>
            {
                EnsureCanRunMoreStatements();
                var resultBuilder = new ResultBuilder(statement, statementParameters, ()=>_connection.ReceiveOne());
                _connection.Run(resultBuilder, statement, statementParameters);
                _connection.PullAll(resultBuilder);
                _connection.Send();

                return resultBuilder.PreBuild();
            });
        }

        public ITransaction BeginTransaction()
        {
            return TryExecute(() =>
            {
                EnsureCanRunMoreStatements();
                _transaction = new Transaction(_connection, _transactionCleanupAction, _logger);
                return _transaction;
            });
        }

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
            if (!_connection.IsHealthy)
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

        public bool IsHealthy
        {
            get
            {
                if (!_connection.IsOpen)
                {
                    return false;
                }
                if (_connection.HasUnrecoverableError)
                {
                    return false;
                }
                return true;
            }
        }

        public void Reset()
        {
            EnsureSessionIsOpen();
            EnsureConnectionIsHealthy();

            _transaction?.MarkToClose();
            _connection.Reset();
            _connection.Send();
        }

        public void Close()
        {
            Dispose(true);
            _connection.Dispose();
        }
    }
}