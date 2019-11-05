// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;
using static Neo4j.Driver.Internal.Util.OptionsBuilder;

namespace Neo4j.Driver.Internal
{
    internal partial class AsyncSession : AsyncStatementRunner, IInternalAsyncSession
    {
        // If the connection is ever successfully created, 
        // then it is session's responsibility to dispose them properly
        // without any possible connection leak.
        private readonly IConnectionProvider _connectionProvider;

        private readonly AccessMode _defaultMode;
        private IConnection _connection;
        private Task<IStatementResultCursor> _result; // last session run result if any

        private AsyncTransaction _transaction;

        private readonly IAsyncRetryLogic _retryLogic;
        private bool _isOpen = true;

        private Bookmark _bookmark;
        private readonly IDriverLogger _logger;

        public Bookmark LastBookmark => _bookmark;

        private readonly string _database;
        private readonly bool _reactive;
        private readonly long _fetchSize;

        public AsyncSession(IConnectionProvider provider, IDriverLogger logger, IAsyncRetryLogic retryLogic = null,
            AccessMode defaultMode = AccessMode.Write, string database = null, Bookmark bookmark = null,
            bool reactive = false, long fetchSize = Config.Infinite)
        {
            _logger = logger;
            _connectionProvider = provider;
            _retryLogic = retryLogic;
            _reactive = reactive;
            _database = database;

            _defaultMode = defaultMode;
            _fetchSize = fetchSize;
            UpdateBookmark(bookmark);
        }

        public Task<IStatementResultCursor> RunAsync(Statement statement, Action<TransactionOptions> optionsBuilder)
        {
            var options = BuildTransactionOptions(optionsBuilder);
            var result = TryExecuteAsync(_logger, async () =>
            {
                await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);
                _connection = await _connectionProvider.AcquireAsync(_defaultMode, _database, _bookmark)
                    .ConfigureAwait(false);
                var protocol = _connection.BoltProtocol;
                return await protocol
                    .RunInAutoCommitTransactionAsync(_connection, statement, _reactive, this, this, _database,
                        _bookmark, options, _fetchSize)
                    .ConfigureAwait(false);
            });

            _result = result;
            return result;
        }
        public Task<IStatementResultCursor> RunAsync(string statement, Action<TransactionOptions> optionsBuilder)
        {
            return RunAsync(new Statement(statement), optionsBuilder);
        }

        public Task<IStatementResultCursor> RunAsync(string statement, IDictionary<string, object> parameters,
            Action<TransactionOptions> optionsBuilder)
        {
            return RunAsync(new Statement(statement, parameters), optionsBuilder);
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            return RunAsync(statement, null);
        }

        public Task<IAsyncTransaction> BeginTransactionAsync()
        {
            return BeginTransactionAsync(null);
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionOptions> optionsBuilder)
        {
            var tx = await TryExecuteAsync(_logger, () => BeginTransactionWithoutLoggingAsync(_defaultMode, optionsBuilder))
                .ConfigureAwait(false);
            return tx;
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode, Action<TransactionOptions> optionsBuilder)
        {
            var tx = await BeginTransactionWithoutLoggingAsync(mode, optionsBuilder).ConfigureAwait(false);
            return tx;
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work)
        {
            return ReadTransactionAsync(work, null);
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work)
        {
            return ReadTransactionAsync(work, null);
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionOptions> optionsBuilder)
        {
            return RunTransactionAsync(AccessMode.Read, work, optionsBuilder);
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionOptions> optionsBuilder)
        {
            return RunTransactionAsync(AccessMode.Read, work, optionsBuilder);
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work)
        {
            return WriteTransactionAsync(work, null);
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work)
        {
            return WriteTransactionAsync(work, null);
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionOptions> optionsBuilder)
        {
            return RunTransactionAsync(AccessMode.Write, work, optionsBuilder);
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionOptions> optionsBuilder)
        {
            return RunTransactionAsync(AccessMode.Write, work, optionsBuilder);
        }

        private Task RunTransactionAsync(AccessMode mode, Func<IAsyncTransaction, Task> work,
            Action<TransactionOptions> optionsBuilder)
        {
            return RunTransactionAsync(mode, async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            }, optionsBuilder);
        }

        private Task<T> RunTransactionAsync<T>(AccessMode mode, Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionOptions> optionsBuilder)
        {
            return TryExecuteAsync(_logger, async () => await _retryLogic.RetryAsync(async () =>
            {
                var tx = await BeginTransactionWithoutLoggingAsync(mode, optionsBuilder).ConfigureAwait(false);
                try
                {
                    var result = await work(tx).ConfigureAwait(false);
                    if (tx.IsOpen)
                    {
                        await tx.CommitAsync().ConfigureAwait(false);
                    }

                    return result;
                }
                catch
                {
                    if (tx.IsOpen)
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                    }

                    throw;
                }
            }).ConfigureAwait(false));
        }

        private async Task<IInternalAsyncTransaction> BeginTransactionWithoutLoggingAsync(AccessMode mode,
            Action<TransactionOptions> optionsBuilder)
        {
            var options = BuildTransactionOptions(optionsBuilder);
            await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);

            _connection = await _connectionProvider.AcquireAsync(mode, _database, _bookmark).ConfigureAwait(false);
            var tx = new AsyncTransaction(_connection, this, _logger, _database, _bookmark, _reactive, _fetchSize);
            await tx.BeginTransactionAsync(options).ConfigureAwait(false);
            _transaction = tx;
            return _transaction;
        }

    }
}