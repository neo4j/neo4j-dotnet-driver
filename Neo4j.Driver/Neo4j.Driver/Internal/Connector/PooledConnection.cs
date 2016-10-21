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
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class PooledConnection : IPooledConnection
    {
        private readonly Action<Guid> _releaseAction;
        private readonly IConnection _connection;

        public void OnReadError(Exception error)
        {
            if (IsRecoverableError(error))
            {
                _connection.AckFailure();
            }
            else
            {
                HasUnrecoverableError = true;
            }
        }
        private bool IsRecoverableError(Exception error)
        {
            return error is ClientException || error is TransientException;
        }

        public bool HasUnrecoverableError { private set; get; }

        public PooledConnection(IConnection connection, Action<Guid> releaseAction = null)
        {
            _connection = connection;
            _releaseAction = releaseAction ?? (x => { });
        }
        public Guid Id { get; } = Guid.NewGuid();

        public void ClearConnection()
        {
            Reset();
            Sync();
        }

        public void Sync()
        {
            _connection.Sync();
        }

        public void Send()
        {
            _connection.Send();
        }

        public void ReceiveOne()
        {
            _connection.ReceiveOne();
        }

        public void Run(string statement, IDictionary<string, object> parameters = null, IMessageResponseCollector resultBuilder = null, bool pullAll = false)
        {
            _connection.Run(statement, parameters, resultBuilder, pullAll);
        }

        public void Reset()
        {
            _connection.Reset();
        }

        public void AckFailure()
        {
            _connection.AckFailure();
        }

        public void ResetAsync()
        {
            _connection.ResetAsync();
        }

        public bool IsOpen => _connection.IsOpen;
        public bool IsHealthy => IsOpen && !HasUnrecoverableError;
        public string Server => _connection.Server;

        /// <summary>
        /// Close the connection and all resources all for good
        /// </summary>
        public void Close()
        {
            _connection.Close();
        }

        /// <summary>
        /// Disposing a pooled connection will try to release the connection resource back to pool
        /// </summary>
        public void Dispose()
        {
            _releaseAction(Id);
        }
    }
}