// Copyright (c) "Neo4j"
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
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;
using static Neo4j.Driver.Internal.Util.ConfigBuilders;

namespace Neo4j.Driver.Internal
{
    internal partial class AsyncSession : AsyncQueryRunner, IInternalAsyncSession
    {
        // If the connection is ever successfully created, 
        // then it is session's responsibility to dispose them properly
        // without any possible connection leak.
        private readonly IConnectionProvider _connectionProvider;

        private readonly AccessMode _defaultMode;
        private IConnection _connection;
        private Task<IResultCursor> _result; // last session run result if any

        private AsyncTransaction _transaction;

        private readonly IAsyncRetryLogic _retryLogic;
        private bool _isOpen = true;
		private bool _disposed = false;

		private Bookmark _bookmark;
        private readonly ILogger _logger;

        public Bookmark LastBookmark => _bookmark;

        private readonly string _database;
        private readonly bool _reactive;
        private readonly long _fetchSize;
		

		public AsyncSession(IConnectionProvider provider, ILogger logger, IAsyncRetryLogic retryLogic = null,
            AccessMode defaultMode = AccessMode.Write,
            string database = null,
            Bookmark bookmark = null, bool reactive = false, long fetchSize = Config.Infinite)
        {
            _connectionProvider = provider;
            _logger = logger;
            _retryLogic = retryLogic;
            _reactive = reactive;

            _database = database;
            _defaultMode = defaultMode;
            _fetchSize = fetchSize;
            UpdateBookmark(bookmark);
        }

        public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action)
        {
            return RunAsync(query, action, true);
        }

        public SessionConfig SessionConfig { internal set; get; }

        public Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action)
        {
            return RunAsync(new Query(query), action);
        }

        public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters,
            Action<TransactionConfigBuilder> action)
        {
            return RunAsync(new Query(query, parameters), action);
        }

        public override Task<IResultCursor> RunAsync(Query query)
        {
            return RunAsync(query, null);
        }

        public Task<IAsyncTransaction> BeginTransactionAsync()
        {
            return BeginTransactionAsync(null);
        }

        public Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action)
        {
            return BeginTransactionAsync(action, true);
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action,
            bool disposeUnconsumedSessionResult)
        {
            var tx = await TryExecuteAsync(_logger,
                    () => BeginTransactionWithoutLoggingAsync(_defaultMode, action, disposeUnconsumedSessionResult))
                .ConfigureAwait(false);
            return tx;
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode,
            Action<TransactionConfigBuilder> action, bool disposeUnconsumedSessionResult)
        {
            var tx = await BeginTransactionWithoutLoggingAsync(mode, action, disposeUnconsumedSessionResult)
                .ConfigureAwait(false);
            return tx;
        }

        public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action,
            bool disposeUnconsumedSessionResult)
        {
            var options = BuildTransactionConfig(action);
            var result = TryExecuteAsync(_logger, async () =>
            {
                await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);
                _connection = await _connectionProvider.AcquireAsync(_defaultMode, _database, _bookmark)
                    .ConfigureAwait(false);
                var protocol = _connection.BoltProtocol;
                return await protocol
                    .RunInAutoCommitTransactionAsync(_connection, query, _reactive, this, this, _database,
                        _bookmark, options, _fetchSize)
                    .ConfigureAwait(false);
            });

            _result = result;
            return result;
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work)
        {
            return ReadTransactionAsync(work, null);
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work)
        {
            return ReadTransactionAsync(work, null);
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionConfigBuilder> action)
        {
            return RunTransactionAsync(AccessMode.Read, work, action);
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action)
        {
            return RunTransactionAsync(AccessMode.Read, work, action);
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work)
        {
            return WriteTransactionAsync(work, null);
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work)
        {
            return WriteTransactionAsync(work, null);
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionConfigBuilder> action)
        {
            return RunTransactionAsync(AccessMode.Write, work, action);
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action)
        {
            return RunTransactionAsync(AccessMode.Write, work, action);
        }

        private Task RunTransactionAsync(AccessMode mode, Func<IAsyncTransaction, Task> work,
            Action<TransactionConfigBuilder> action)
        {
            return RunTransactionAsync(mode, async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            }, action);
        }

        private Task<T> RunTransactionAsync<T>(AccessMode mode, Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return TryExecuteAsync(_logger, async () => await _retryLogic.RetryAsync(async () =>
            {
                var tx = await BeginTransactionWithoutLoggingAsync(mode, action, true).ConfigureAwait(false);
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
            Action<TransactionConfigBuilder> action, bool disposeUnconsumedSessionResult)
        {
            var config = BuildTransactionConfig(action);
            await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);

            _connection = await _connectionProvider.AcquireAsync(mode, _database, _bookmark).ConfigureAwait(false);
            var tx = new AsyncTransaction(_connection, this, _logger, _database, _bookmark, _reactive, _fetchSize);
            await tx.BeginTransactionAsync(config).ConfigureAwait(false);
            _transaction = tx;
            return _transaction;
        }

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if(disposing)
			{
				//Dispose managed resources
				
				//call it synchronously
				//CloseAsync().GetAwaiter().GetResult();
				Task.Run(() => CloseAsync()).GetAwaiter().GetResult();
			}

			_disposed = true;
			base.Dispose(disposing);
		}

		protected override async ValueTask DisposeAsyncCore()
		{
			await CloseAsync().ConfigureAwait(false);
			await base.DisposeAsyncCore();
		}
    }
}