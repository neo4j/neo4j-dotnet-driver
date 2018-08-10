// Copyright (c) 2002-2018 "Neo4j,"
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
                var protocol = _connection.BoltProtocol;
                return protocol.RunInAutoCommitTransaction( _connection, statement, this);
            });
        }

        public override Task<IStatementResultCursor> RunAsync(Statement statement)
        {
            return TryExecuteAsync(async () =>
            {
                await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);
                _connection = await _connectionProvider.AcquireAsync(_defaultMode).ConfigureAwait(false);
                var protocol = _connection.BoltProtocol;
                return await protocol.RunInAutoCommitTransactionAsync(_connection, statement, this).ConfigureAwait(false);
            });
        }

        public ITransaction BeginTransaction()
        {
            return TryExecute(() => BeginTransactionWithoutLogging(_defaultMode));
        }

        public Task<ITransaction> BeginTransactionAsync()
        {
            return TryExecuteAsync(() => BeginTransactionWithoutLoggingAsync(_defaultMode));
        }

        public ITransaction BeginTransaction(string bookmark)
        {
            UpdateBookmark(Bookmark.From(bookmark, Logger));
            return BeginTransaction();
        }

        public void ReadTransaction(Action<ITransaction> work)
        {
            RunTransaction(AccessMode.Read, work);
        }

        public Task ReadTransactionAsync(Func<ITransaction, Task> work)
        {
            return RunTransactionAsync(AccessMode.Read, work);
        }

        public T ReadTransaction<T>(Func<ITransaction, T> work)
        {
            return RunTransaction(AccessMode.Read, work);
        }

        public Task<T> ReadTransactionAsync<T>(Func<ITransaction, Task<T>> work)
        {
            return RunTransactionAsync(AccessMode.Read, work);
        }

        public void WriteTransaction(Action<ITransaction> work)
        {
            RunTransaction(AccessMode.Write, work);
        }

        public Task WriteTransactionAsync(Func<ITransaction, Task> work)
        {
            return RunTransactionAsync(AccessMode.Write, work);
        }

        public T WriteTransaction<T>(Func<ITransaction, T> work)
        {
            return RunTransaction(AccessMode.Write, work);
        }

        public Task<T> WriteTransactionAsync<T>(Func<ITransaction, Task<T>> work)
        {
            return RunTransactionAsync(AccessMode.Write, work);
        }

        private void RunTransaction(AccessMode mode, Action<ITransaction> work)
        {
            RunTransaction<object>(mode, tx =>
            {
                work(tx);
                return null;
            });
        }

        private T RunTransaction<T>(AccessMode mode, Func<ITransaction, T> work)
        {
            return TryExecute(() => _retryLogic.Retry(() =>
            {
                using (var tx = BeginTransactionWithoutLogging(mode))
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

        private Task RunTransactionAsync(AccessMode mode, Func<ITransaction, Task> work)
        {
            return RunTransactionAsync(mode, async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            });
        }

        private Task<T> RunTransactionAsync<T>(AccessMode mode, Func<ITransaction, Task<T>> work)
        {
            return TryExecuteAsync(async () => await _retryLogic.RetryAsync(async () =>
            {
                var tx = await BeginTransactionWithoutLoggingAsync(mode).ConfigureAwait(false);
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

        private ITransaction BeginTransactionWithoutLogging(AccessMode mode)
        {
            EnsureCanRunMoreStatements();

            _connection = _connectionProvider.Acquire(mode);
            var tx = new Transaction(_connection, this, Logger, _bookmark);
            tx.BeginTransaction();
            _transaction = tx;
            return _transaction;
        }

        private async Task<ITransaction> BeginTransactionWithoutLoggingAsync(AccessMode mode)
        {
            await EnsureCanRunMoreStatementsAsync().ConfigureAwait(false);

            _connection = await _connectionProvider.AcquireAsync(mode).ConfigureAwait(false);
            var tx = new Transaction(_connection, this, Logger, _bookmark);
            await tx.BeginTransactionAsync().ConfigureAwait(false);
            _transaction = tx;
            return _transaction;
        }
    }
}
