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
        private readonly IDriverLogger _logger;
        private readonly SyncExecutor _syncExecutor;
        public string LastBookmark => _bookmark?.MaxBookmark;

        public Session(IConnectionProvider provider, IDriverLogger logger, SyncExecutor syncExecutor,
            IRetryLogic retryLogic = null,
            AccessMode defaultMode = AccessMode.Write, Bookmark bookmark = null)
        {
            _logger = logger;
            _connectionProvider = provider;
            _retryLogic = retryLogic;
            _syncExecutor = syncExecutor;

            _defaultMode = defaultMode;
            UpdateBookmark(bookmark);
        }

        public IStatementResult Run(Statement statement, TransactionConfig txConfig)
        {
            return new StatementResult(_syncExecutor.RunSync(() => RunAsync(statement, txConfig)), _syncExecutor);
        }

        public Task<IStatementResultCursor> RunAsync(Statement statement, TransactionConfig txConfig)
        {
            return RunAsync(statement, true, txConfig);
        }

        internal Task<IStatementResultCursor> RunAsync(Statement statement, bool pullAll, TransactionConfig txConfig)
        {
            return TryExecuteAsync(_logger, async () =>
            {
                await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);
                _connection = await _connectionProvider.AcquireAsync(_defaultMode).ConfigureAwait(false);
                var protocol = _connection.BoltProtocol;
                return await protocol
                    .RunInAutoCommitTransactionAsync(_connection, statement, pullAll, this, this, _bookmark, txConfig)
                    .ConfigureAwait(false);
            });
        }

        public IStatementResult Run(string statement, TransactionConfig txConfig)
        {
            return Run(new Statement(statement), txConfig);
        }

        public Task<IStatementResultCursor> RunAsync(string statement, TransactionConfig txConfig)
        {
            return RunAsync(new Statement(statement), txConfig);
        }

        public IStatementResult Run(string statement, IDictionary<string, object> parameters,
            TransactionConfig txConfig)
        {
            return Run(new Statement(statement, parameters), txConfig);
        }

        public Task<IStatementResultCursor> RunAsync(string statement, IDictionary<string, object> parameters,
            TransactionConfig txConfig)
        {
            return RunAsync(new Statement(statement, parameters), txConfig);
        }

        public override IStatementResult Run(Statement statement)
        {
            return Run(statement, TransactionConfig.Empty);
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            return RunAsync(statement, TransactionConfig.Empty);
        }

        public ITransaction BeginTransaction()
        {
            return BeginTransaction((TransactionConfig) null);
        }

        public Task<ITransaction> BeginTransactionAsync()
        {
            return BeginTransactionAsync(null);
        }

        public ITransaction BeginTransaction(TransactionConfig txConfig)
        {
            return TryExecute(_logger, () => BeginTransactionWithoutLogging(_defaultMode, txConfig));
        }

        public Task<ITransaction> BeginTransactionAsync(TransactionConfig txConfig)
        {
            return TryExecuteAsync(_logger, () => BeginTransactionWithoutLoggingAsync(_defaultMode, txConfig));
        }

        public ITransaction BeginTransaction(string bookmark)
        {
            UpdateBookmark(Bookmark.From(bookmark));
            return BeginTransaction();
        }

        public T ReadTransaction<T>(Func<ITransaction, T> work)
        {
            return ReadTransaction(work, null);
        }

        public Task<T> ReadTransactionAsync<T>(Func<ITransaction, Task<T>> work)
        {
            return ReadTransactionAsync(work, null);
        }

        public void ReadTransaction(Action<ITransaction> work)
        {
            ReadTransaction(work, null);
        }

        public Task ReadTransactionAsync(Func<ITransaction, Task> work)
        {
            return ReadTransactionAsync(work, null);
        }

        public T ReadTransaction<T>(Func<ITransaction, T> work, TransactionConfig txConfig)
        {
            return RunTransaction(AccessMode.Read, work, txConfig);
        }

        public Task<T> ReadTransactionAsync<T>(Func<ITransaction, Task<T>> work, TransactionConfig txConfig)
        {
            return RunTransactionAsync(AccessMode.Read, work, txConfig);
        }

        public void ReadTransaction(Action<ITransaction> work, TransactionConfig txConfig)
        {
            RunTransaction(AccessMode.Read, work, txConfig);
        }

        public Task ReadTransactionAsync(Func<ITransaction, Task> work, TransactionConfig txConfig)
        {
            return RunTransactionAsync(AccessMode.Read, work, txConfig);
        }

        public T WriteTransaction<T>(Func<ITransaction, T> work)
        {
            return WriteTransaction(work, null);
        }

        public Task<T> WriteTransactionAsync<T>(Func<ITransaction, Task<T>> work)
        {
            return WriteTransactionAsync(work, null);
        }

        public void WriteTransaction(Action<ITransaction> work)
        {
            WriteTransaction(work, null);
        }

        public Task WriteTransactionAsync(Func<ITransaction, Task> work)
        {
            return WriteTransactionAsync(work, null);
        }

        public T WriteTransaction<T>(Func<ITransaction, T> work, TransactionConfig txConfig)
        {
            return RunTransaction(AccessMode.Write, work, txConfig);
        }

        public Task<T> WriteTransactionAsync<T>(Func<ITransaction, Task<T>> work, TransactionConfig txConfig)
        {
            return RunTransactionAsync(AccessMode.Write, work, txConfig);
        }

        public void WriteTransaction(Action<ITransaction> work, TransactionConfig txConfig)
        {
            RunTransaction(AccessMode.Write, work, txConfig);
        }

        public Task WriteTransactionAsync(Func<ITransaction, Task> work, TransactionConfig txConfig)
        {
            return RunTransactionAsync(AccessMode.Write, work, txConfig);
        }

        private void RunTransaction(AccessMode mode, Action<ITransaction> work, TransactionConfig txConfig)
        {
            RunTransaction<object>(mode, tx =>
            {
                work(tx);
                return null;
            }, txConfig);
        }

        private T RunTransaction<T>(AccessMode mode, Func<ITransaction, T> work, TransactionConfig txConfig)
        {
            return TryExecute(_logger, () => _retryLogic.Retry(() =>
            {
                using (var tx = BeginTransactionWithoutLogging(mode, txConfig))
                {
                    try
                    {
                        var result = work(tx);
                        tx.Success();
                        return result;
                    }
                    catch
                    {
                        tx.Failure();
                        throw;
                    }
                }
            }));
        }

        private Task RunTransactionAsync(AccessMode mode, Func<ITransaction, Task> work, TransactionConfig txConfig)
        {
            return RunTransactionAsync(mode, async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            }, txConfig);
        }

        private Task<T> RunTransactionAsync<T>(AccessMode mode, Func<ITransaction, Task<T>> work,
            TransactionConfig txConfig)
        {
            return TryExecuteAsync(_logger, async () => await _retryLogic.RetryAsync(async () =>
            {
                var tx = await BeginTransactionWithoutLoggingAsync(mode, txConfig).ConfigureAwait(false);
                {
                    try
                    {
                        var result = await work(tx).ConfigureAwait(false);
                        await tx.CommitAsync().ConfigureAwait(false);
                        return result;
                    }
                    catch
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                        throw;
                    }
                }
            }).ConfigureAwait(false));
        }

        private ITransaction BeginTransactionWithoutLogging(AccessMode mode, TransactionConfig txConfig)
        {
            return _syncExecutor.RunSync(() => BeginTransactionWithoutLoggingAsync(mode, txConfig));
        }

        private async Task<ITransaction> BeginTransactionWithoutLoggingAsync(AccessMode mode,
            TransactionConfig txConfig)
        {
            await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);

            _connection = await _connectionProvider.AcquireAsync(mode).ConfigureAwait(false);
            var tx = new Transaction(_connection, _syncExecutor, this, _logger, _bookmark);
            await tx.BeginTransactionAsync(txConfig ?? TransactionConfig.Empty).ConfigureAwait(false);
            _transaction = tx;
            return _transaction;
        }
    }
}