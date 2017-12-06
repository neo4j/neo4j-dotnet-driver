﻿// Copyright (c) 2002-2017 "Neo Technology,"
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal partial class Session : StatementRunner, ISession
    {
        // If the connection is ever successfully created, 
        // then it is session's responsibility to dispose them properly
        // without any possible connection leak.
        private readonly IConnectionProvider _connectionProvider;
        private readonly AccessMode _defaultMode;
        private IConnection _connection;

        private Transaction _transaction;

        private readonly IRetryLogic _retryLogic;
        private bool _isOpen = true;

        private Bookmark _bookmark;
        public string LastBookmark => _bookmark?.MaxBookmarkAsString();

        public Guid Id { get; } = Guid.NewGuid();

        public Session(IConnectionProvider provider, ILogger logger, IRetryLogic retryLogic = null,
            AccessMode defaultMode = AccessMode.Write, Bookmark bookmark = null) : base(logger)
        {
            _connectionProvider = provider;
            _retryLogic = retryLogic;

            _defaultMode = defaultMode;
            UpdateBookmark(bookmark);
        }

        public override IStatementResult Run(Statement statement)
        {
            return TryExecute(() =>
            {
                EnsureCanRunMoreStatements();

                _connection = _connectionProvider.Acquire(_defaultMode);
                var resultBuilder = new ResultBuilder(statement.Text, statement.Parameters,
                    () => _connection.ReceiveOne(), _connection.Server, this);
                _connection.Run(statement.Text, statement.Parameters, resultBuilder);
                _connection.Send();

                return resultBuilder.PreBuild();
            });
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            return TryExecuteAsync(async () =>
            {
                await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);

                _connection = await _connectionProvider.AcquireAsync(_defaultMode).ConfigureAwait(false);
                var resultBuilder = new ResultCursorBuilder(statement.Text, statement.Parameters,
                    () => _connection.ReceiveOneAsync(), _connection.Server, this);
                _connection.Run(statement.Text, statement.Parameters, resultBuilder);

                await _connection.SendAsync().ConfigureAwait(false);

                return await resultBuilder.PreBuildAsync().ConfigureAwait(false);
            });
        }

        public ITransaction BeginTransaction()
        {
            return TryExecute(() => BeginTransactionWithoutLogging(_defaultMode));
        }

        public ITransaction BeginTransaction(TimeSpan timeout)
        {
            return TryExecute(() => BeginTransactionWithoutLogging(_defaultMode, timeout));
        }

        public ITransaction BeginTransaction(string bookmark)
        {
            UpdateBookmark(Bookmark.From(bookmark, Logger));
            return BeginTransaction();
        }

        private ITransaction BeginTransactionWithoutLogging(AccessMode mode, TimeSpan? timeout = null)
        {
            EnsureCanRunMoreStatements();

            _connection = _connectionProvider.Acquire(mode);
            var tx = new Transaction(_connection, this, Logger, _bookmark, timeout);
            tx.SyncBookmark(_bookmark);
            _transaction = tx;
            return _transaction;
        }

        public Task<ITransaction> BeginTransactionAsync()
        {
            return TryExecuteAsync(() => BeginTransactionWithoutLoggingAsync(_defaultMode));
        }

        public Task<ITransaction> BeginTransactionAsync(TimeSpan timeout)
        {
            return TryExecuteAsync(() => BeginTransactionWithoutLoggingAsync(_defaultMode, timeout));
        }

        private async Task<ITransaction> BeginTransactionWithoutLoggingAsync(AccessMode mode, TimeSpan? timeout = null)
        {
            await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);

            _connection = await _connectionProvider.AcquireAsync(mode).ConfigureAwait(false);
            var tx = new Transaction(_connection, this, Logger, _bookmark, timeout);
            await tx.SyncBookmarkAsync(_bookmark).ConfigureAwait(false);
            _transaction = tx;
            return _transaction;
        }

        /// <summary>
        /// Only set the bookmark to a new value if the new value is not null
        /// </summary>
        /// <param name="bookmark">The new bookmark</param>
        private void UpdateBookmark(Bookmark bookmark)
        {
            if (bookmark != null && !bookmark.IsEmpty())
            {
                _bookmark = bookmark;
            }
        }
    }
}
