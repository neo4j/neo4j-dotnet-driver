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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class PooledConnection : DelegatedConnection, IPooledConnection
    {
        private readonly Action<IPooledConnection> _releaseAction;

        public PooledConnection(IConnection conn, Action<IPooledConnection> releaseAction = null)
            :base (conn)
        {
            _releaseAction = releaseAction ?? (x => { });
        }
        public Guid Id { get; } = Guid.NewGuid();

        public void ClearConnection()
        {
            Reset();
            Sync();
        }

        public override bool IsOpen => Delegate.IsOpen && !HasUnrecoverableError;

        /// <summary>
        /// Disposing a pooled connection will try to release the connection resource back to pool
        /// </summary>
        public override void Dispose()
        {
            _releaseAction(this);
        }

        /// <summary>
        /// Return true if unrecoverable error has been received on this connection, otherwise false.
        /// The connection that has been marked as has unrecoverable errors will be eventally closed when returning back to the pool.
        /// </summary>
        internal bool HasUnrecoverableError { private set; get; }

        public override void OnError(Exception error)
        {
            if (error.IsRecoverableError())
            {
                Delegate.AckFailure();
            }
            else
            {
                HasUnrecoverableError = true;
            }

            if (error.IsConnectionError())
            {
                throw new ServiceUnavailableException(
                    $"Connection with the server breaks due to {error.GetType().Name}: {error.Message}", error);
            }
            else
            {
                throw error;
            }
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}