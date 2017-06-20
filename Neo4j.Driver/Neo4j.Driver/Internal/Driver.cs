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
using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class Driver : IDriver
    {
        private volatile bool _disposeCalled = false;

        private readonly IConnectionProvider _connectionProvider;
        private readonly IRetryLogic _retryLogic;
        private ILogger _logger;
        public Uri Uri { get; }

        internal Driver(Uri uri, IConnectionProvider connectionProvider, IRetryLogic retryLogic, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(connectionProvider, nameof(connectionProvider));

            Uri = uri;
            _logger = logger;
            _connectionProvider = connectionProvider;
            _retryLogic = retryLogic;
        }

        public ISessionAsync Session(AccessMode defaultMode=AccessMode.Write, string bookmark = null)
        {
            return Session(defaultMode, Bookmark.From(bookmark, _logger));
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }
            _disposeCalled = true;

            // We cannot set connection pool to be null,
            // otherwise we might get NPE when using concurrently with NewSession
            _connectionProvider.Dispose();
            _logger = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ThrowDriverClosedException()
        {
            throw new ObjectDisposedException(GetType().Name, "Cannot open a new session on a driver that is already disposed.");
        }

        public ISessionAsync Session(AccessMode defaultMode, IEnumerable<string> bookmarks)
        {
            return Session(defaultMode, Bookmark.From(bookmarks));
        }

        public ISessionAsync Session(IEnumerable<string> bookmarks)
        {
            return Session(AccessMode.Write, bookmarks);
        }

        private ISessionAsync Session(AccessMode defaultMode, Bookmark bookmark)
        {
            if (_disposeCalled)
            {
                ThrowDriverClosedException();
            }

            var session = new Session(_connectionProvider, _logger, _retryLogic, defaultMode, bookmark);

            if (_disposeCalled)
            {
                session.Dispose();
                ThrowDriverClosedException();
            }
            return session;
        }
    }
}