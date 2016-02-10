//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    public class Driver : LoggerBase, IDisposable
    {
        private readonly Config _config;
        private readonly Uri _uri;
        public Uri Uri => _uri;
        private SessionPool _sessionPool;

        internal Driver(Uri uri, Config config) : base(config?.Logger)
        {
            if (uri.Scheme.ToLowerInvariant() == "bolt" && uri.Port == -1)
            {
                var builder = new UriBuilder(uri.Scheme, uri.Host, 7687);
                uri = builder.Uri;
            }

            _uri = uri;
            _config = config;
            _sessionPool = new SessionPool(config.Logger, uri, config);
        }

        protected override void Dispose(bool isDisposing)
        {
           
            if (!isDisposing)
                return;
            _sessionPool?.Dispose();
            _sessionPool = null;
            Logger?.Dispose();
        }

        /// <summary>
        ///     Establish a session with Neo4j instance
        /// </summary>
        /// <returns>
        ///     An <see cref="ISession" /> that could be used to <see cref="ISession.Run" /> a statement or begin a
        ///     transaction
        /// </returns>
        public ISession Session()
        {
            return _sessionPool.GetSession();
        }

    }
}