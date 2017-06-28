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
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.Throw.ArgumentNullException;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionSettings
    {
        internal const string DefaultUserAgent = "neo4j-dotnet/1.4";

        public Uri InitialServerUri { get; }
        public IAuthToken AuthToken { get; }
        public TimeSpan ConnectionTimeout { get; }
        public string UserAgent { get; }
        public EncryptionManager EncryptionManager { get; }
        public bool SocketKeepAliveEnabled { get; }
        public bool Ipv6Enabled { get; }

        public ConnectionSettings(Uri uri, IAuthToken auth, Config config)
        : this(uri, auth, new EncryptionManager(config.EncryptionLevel, config.TrustStrategy, config.Logger),
              config.ConnectionTimeout, config.SocketKeepAlive, config.Ipv6Enabled)
        {
        }

        private ConnectionSettings(Uri initialServerUri, IAuthToken authToken, 
            EncryptionManager encryptionManager, TimeSpan connectionTimeout, 
            bool socketKeepAlive, bool ipv6Enabled, string userAgent = null)
        {
            IfNull(initialServerUri, nameof(initialServerUri));
            IfNull(authToken, nameof(authToken));
            IfNull(encryptionManager, nameof(encryptionManager));
            IfNull(connectionTimeout, nameof(connectionTimeout));
            IfNull(socketKeepAlive, nameof(socketKeepAlive));
            IfNull(ipv6Enabled, nameof(ipv6Enabled));

            InitialServerUri = initialServerUri;
            AuthToken = authToken;
            EncryptionManager = encryptionManager;
            ConnectionTimeout = connectionTimeout;
            UserAgent = userAgent ?? DefaultUserAgent;
            SocketKeepAliveEnabled = socketKeepAlive;
            Ipv6Enabled = ipv6Enabled;
        }
    }
}