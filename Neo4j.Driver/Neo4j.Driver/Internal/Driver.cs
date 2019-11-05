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
using System.Linq;
using System.Threading;
using Neo4j.Driver;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal
{
    internal class Driver : IInternalDriver
    {
        private int _closedMarker = 0;

        private readonly IConnectionProvider _connectionProvider;
        private readonly IAsyncRetryLogic _retryLogic;
        private readonly IDriverLogger _logger;
        private readonly IMetrics _metrics;
        private readonly Config _config;

        public Uri Uri { get; }

        internal Driver(Uri uri, IConnectionProvider connectionProvider, IAsyncRetryLogic retryLogic,
            IDriverLogger logger = null,
            IMetrics metrics = null, Config config = null)
        {
            Throw.ArgumentNullException.IfNull(connectionProvider, nameof(connectionProvider));

            Uri = uri;
            _logger = logger;
            _connectionProvider = connectionProvider;
            _retryLogic = retryLogic;
            _metrics = metrics;
            _config = config;
        }

        private bool IsClosed => _closedMarker > 0;

        public Config Config => _config;

        public IAsyncSession AsyncSession()
        {
            return AsyncSession(null);
        }

        public IAsyncSession AsyncSession(Action<SessionOptions> optionsBuilder)
        {
            return Session(optionsBuilder, false);
        }

        public IInternalAsyncSession Session(Action<SessionOptions> optionsBuilder, bool reactive)
        {
            if (IsClosed)
            {
                ThrowDriverClosedException();
            }

            var options = OptionsBuilder.BuildSessionOptions(optionsBuilder);

            var session = new AsyncSession(_connectionProvider, _logger, _retryLogic, options.DefaultAccessMode,
                options.Database, Bookmark.From(options.Bookmarks ?? Array.Empty<Bookmark>()), reactive,
                ParseFetchSize(options.FetchSize));

            if (IsClosed)
            {
                ThrowDriverClosedException();
            }

            return session;
        }

        private long ParseFetchSize(long? fetchSize)
        {
            return fetchSize.GetValueOrDefault(_config.FetchSize);
        }

        private void Close()
        {
            Task.Factory
                .StartNew(CloseAsync, TaskCreationOptions.None)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                return _connectionProvider.CloseAsync();
            }

            return Task.CompletedTask;
        }

        public Task VerifyConnectivityAsync()
        {
            return _connectionProvider.VerifyConnectivityAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsClosed)
                return;

            if (disposing)
            {
                Close();
            }
        }

        private void ThrowDriverClosedException()
        {
            throw new ObjectDisposedException(GetType().Name,
                "Cannot open a new session on a driver that is already disposed.");
        }

        internal IMetrics GetMetrics()
        {
            if (_metrics == null)
            {
                throw new InvalidOperationException(
                    "Cannot access driver metrics if it is not enabled when creating this driver.");
            }

            return _metrics;
        }
    }
}