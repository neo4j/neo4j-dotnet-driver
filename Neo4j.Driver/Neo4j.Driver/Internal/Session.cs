//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal
{
    public class Session : LoggerBase, IPooledSession
    {
        private readonly IConnection _connection;
        private Transaction _transaction;
        private readonly Action<Guid> _releaseAction;

        public Session(Uri url, Config config, IConnection conn = null, Action<Guid> releaseAction = null )
            : base(config?.Logger)
        {
            _connection = TryExecute(() => conn ?? new SocketConnection(url, config));
            _releaseAction = releaseAction ?? (x => {}); 
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            if(_transaction!= null && !_transaction.Finished)
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

            _releaseAction(Id);
            base.Dispose(isDisposing);
            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ResultCursor Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            return TryExecute(() =>
            {
                EnsureConnectionIsValid();
                var resultBuilder = new ResultBuilder(statement, statementParameters);
                _connection.Run(resultBuilder, statement, statementParameters);
                _connection.PullAll(resultBuilder);
                _connection.Sync();

                return resultBuilder.Build();
            });
        }

        public ITransaction BeginTransaction()
        {
            return TryExecute(() =>
            {
                EnsureConnectionIsValid();
                _transaction = new Transaction(_connection);
                return _transaction;
            });
        }

        private void EnsureConnectionIsValid()
        {
            EnsureNoOpenTransaction();
            EnsureConnectionIsOpen();
        }

        private void EnsureConnectionIsOpen()
        {
            if (!_connection.IsOpen)
            {
                throw new ClientException("The current session cannot be reused as the underlying connection with the " +
                                           "server has been closed due to unrecoverable errors. " +
                                           "Please close this session and retry your statement in another new session.");
            }
        }

        private void EnsureNoOpenTransaction()
        {
            if (_transaction == null)
            {
                return;
            }
            if (!_transaction.Finished)
            {
                throw new ClientException("Please close the currently open transaction object before running " +
                                           "more statements/transactions in the current session.");
            }
        }

        public Guid Id { get; } = Guid.NewGuid();
        public bool IsHealthy()
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

        public void Reset()
        {
            _connection.Reset();
            _connection.Sync();
        }

        public void Close()
        {
            Dispose(true);
            _connection.Dispose();
        }
    }
}