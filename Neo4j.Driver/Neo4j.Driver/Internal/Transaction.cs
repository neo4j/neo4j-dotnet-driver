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
    public class Transaction : ITransaction
    {
        private State _state = State.Active;
        private readonly IConnection _connection;
        public bool Finished { get; private set; }

        public Transaction(IConnection connection)
        {
            _connection = connection;
            Finished = false;

            _connection.Run(null, "BEGIN");
            _connection.DiscardAll();
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

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }
            try
            {
                if (_state == State.MarkedSuccess)
                {
                    _connection.Run(null, "COMMIT");
                    _connection.DiscardAll();
                    _connection.Sync();
                    _state = State.Succeeded;
                }
                else if (_state == State.MarkedFailed || _state == State.Active)
                {
                    // If alwaysValid of the things we've put in the queue have been sent off, there is no need to
                    // do this, we could just clear the queue. Future optimization.
                    _connection.Run(null, "ROLLBACK");
                    _connection.DiscardAll();
                    _state = State.RolledBack;
                }
            }
            finally
            {
                Finished = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IResultCursor Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            EnsureNotFailed();

            try
            {
                ResultBuilder resultBuilder = new ResultBuilder(statement, statementParameters);
                _connection.Run(resultBuilder, statement, statementParameters);
                _connection.PullAll(resultBuilder);
                _connection.Sync();
                return resultBuilder.Build();
            }
            catch (Neo4jException)
            {
                _state = State.Failed;
                throw;
            }
        }

        private void EnsureNotFailed()
        {
            if (_state == State.Failed)
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
    }
}