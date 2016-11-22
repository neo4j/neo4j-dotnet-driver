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
    internal class Transaction : StatementRunner, ITransaction
    {
        private readonly IConnection _connection;
        private readonly Action _cleanupAction;

        internal const string BookmarkKey = "bookmark";
        internal string Bookmark { get; private set; }

        private const string Begin = "BEGIN";
        private const string Commit = "COMMIT";
        private const string Rollback = "ROLLBACK";

        /* 
         * All the blocks that modifies the state of this tx and perform certain actoin based on the current tx state should be syncronized
         * as a reset thread and a run thread could modify this state at the same time.
         */
        private State _state = State.Active;
        private readonly object _syncLock = new object();

        public Transaction(IConnection connection, Action cleanupAction=null, ILogger logger=null, string bookmark = null) : base(logger)
        {
            _connection = connection;
            _cleanupAction = cleanupAction ?? (() => { });

            IDictionary<string, object> paramters = new Dictionary<string, object>();
            if (bookmark != null)
            {
                paramters.Add(BookmarkKey, bookmark);
            }
            _connection.Run(Begin, paramters);
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
                lock (_syncLock)
                {
                    if (_state == State.MarkedSuccess)
                    {
                        _connection.Run(Commit, null, new BookmarkCollector(s => Bookmark = s));
                        _connection.Sync();
                        _state = State.Succeeded;
                    }
                    else if (_state == State.MarkedFailed || _state == State.Active)
                    {
                        // If alwaysValid of the things we've put in the queue have been sent off, there is no need to
                        // do this, we could just clear the queue. Future optimization.
                        _connection.Run(Rollback, null, new BookmarkCollector(s => Bookmark = s));
                        _connection.Sync();
                        _state = State.RolledBack;
                    }
                }
            }
            finally
            {
                _cleanupAction.Invoke();;
                base.Dispose(true);
            }
        }

        public override IStatementResult Run(string statement, IDictionary<string, object> parameters=null)
        {
            return TryExecute(() =>
            {
                lock (_syncLock)
                {
                    EnsureNotFailed();
                    try
                    {
                        var resultBuilder = new ResultBuilder(statement, parameters, () => _connection.ReceiveOne(), _connection.Server);
                        _connection.Run(statement, parameters, resultBuilder);
                        _connection.Send();
                        return resultBuilder.PreBuild();
                    }
                    catch (Neo4jException)
                    {
                        _state = State.Failed;
                        throw;
                    }
                }
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
            lock (_syncLock)
            {
                if (_state == State.Active)
                {
                    _state = State.MarkedSuccess;
                }
            }
        }

        public void Failure()
        {
            lock (_syncLock)
            {
                if (_state == State.Active || _state == State.MarkedSuccess)
                {
                    _state = State.MarkedFailed;
                }
            }
        }

        public void MarkToClose()
        {
            lock (_syncLock)
            {
                _state = State.Failed;
            }
        }
    }
}