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

namespace Neo4j.Driver.Internal
{
    internal class Driver : IDriver
    {
        private SessionPool _sessionPool;

        internal Driver(Uri uri, IAuthToken authToken, Config config)
        {
            Throw.ArgumentNullException.IfNull(uri, nameof(uri));
            Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
            Throw.ArgumentNullException.IfNull(config, nameof(config));

            if (uri.Port == -1)
            {
                var builder = new UriBuilder(uri.Scheme, uri.Host, 7687);
                uri = builder.Uri;
            }

            Uri = uri;
            _sessionPool = new SessionPool(uri, authToken, config?.Logger, config);
        }

        public Uri Uri { get; }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            if (_sessionPool != null)
            {
                _sessionPool.Dispose();
                _sessionPool = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ISession Session()
        {
            return _sessionPool.GetSession();
        }
    }
}