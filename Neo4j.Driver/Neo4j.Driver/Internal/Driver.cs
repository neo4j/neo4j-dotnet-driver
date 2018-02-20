// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.Threading;
using Neo4j.Driver.V1;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;

namespace Neo4j.Driver.Internal
{
    internal class Driver : IDriver
    {
        private int _closedMarker = 0;

        private readonly IConnectionProvider _connectionProvider;
        private readonly IRetryLogic _retryLogic;
        private readonly ILogger _logger;
        private readonly IDriverMetrics _driverMetrics;
        public Uri Uri { get; }

        private const AccessMode DefaultAccessMode = AccessMode.Write;
        private const string NullBookmark = null;

        internal Driver(Uri uri, IConnectionProvider connectionProvider, IRetryLogic retryLogic, ILogger logger,
            IDriverMetrics driverMetrics=null)
        {
            Throw.ArgumentNullException.IfNull(connectionProvider, nameof(connectionProvider));

            Uri = uri;
            _logger = logger;
            _connectionProvider = connectionProvider;
            _retryLogic = retryLogic;
            _driverMetrics = driverMetrics;
        }

        private bool IsClosed => _closedMarker > 0;

        public ISession Session()
        {
            return Session(DefaultAccessMode);
        }

        public ISession Session(AccessMode defaultMode)
        {
            return Session(defaultMode, NullBookmark);
        }

        public ISession Session(string bookmark)
        {
            return Session(DefaultAccessMode, bookmark);
        }


        public ISession Session(AccessMode defaultMode, string bookmark)
        {
            return Session(defaultMode, Bookmark.From(bookmark, _logger));
        }

       
        public ISession Session(AccessMode defaultMode, IEnumerable<string> bookmarks)
        {
            return Session(defaultMode, Bookmark.From(bookmarks, _logger));
        }

        public ISession Session(IEnumerable<string> bookmarks)
        {
            return Session(AccessMode.Write, bookmarks);
        }

        private ISession Session(AccessMode defaultMode, Bookmark bookmark)
        {
            if (IsClosed)
            {
                ThrowDriverClosedException();
            }

            var session = new Session(_connectionProvider, _logger, _retryLogic, defaultMode, bookmark);

            if (IsClosed)
            {
                session.Dispose();
                ThrowDriverClosedException();
            }

            return session;
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                _connectionProvider.Close();
            }
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                return _connectionProvider.CloseAsync();
            }

            return TaskUtils.GetCompletedTask();
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
            throw new ObjectDisposedException(GetType().Name, "Cannot open a new session on a driver that is already disposed.");
        }

        internal IDriverMetrics GetDriverMetrics()
        {
            if (_driverMetrics == null)
            {
                throw new InvalidOperationException("Cannot access driver metrics if it is not enabled when creating this driver.");
            }
            return _driverMetrics;
        }
    }
}
