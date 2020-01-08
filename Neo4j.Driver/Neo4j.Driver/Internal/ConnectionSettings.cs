// Copyright (c) 2002-2020 "Neo4j,"
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
using Neo4j.Driver.Internal.Connector;
using static Neo4j.Driver.Internal.Throw.ArgumentNullException;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionSettings
    {
        internal const string DefaultUserAgent = "neo4j-dotnet/1.7";

        public IAuthToken AuthToken { get; }
        public string UserAgent { get; }
        public SocketSettings SocketSettings { get; }

        public ConnectionSettings(IAuthToken auth, Config config)
        : this(auth, new EncryptionManager(config.EncryptionLevel, config.TrustManager, config.Logger),
              config.ConnectionTimeout, config.SocketKeepAlive, config.Ipv6Enabled)
        {
        }

        private ConnectionSettings(IAuthToken authToken,
            EncryptionManager encryptionManager, TimeSpan connectionTimeout, 
            bool socketKeepAlive, bool ipv6Enabled, string userAgent = null)
        {
            IfNull(authToken, nameof(authToken));
            IfNull(encryptionManager, nameof(encryptionManager));

            AuthToken = authToken;
            UserAgent = userAgent ?? DefaultUserAgent;

            IHostResolver systemResolver = new SystemHostResolver();
            if (RuntimeHelper.IsDotnetCore())
            {
                systemResolver = new SystemNetCoreHostResolver(systemResolver);
            }
            
            SocketSettings = new SocketSettings
            {
                HostResolver = new DefaultHostResolver(systemResolver, ipv6Enabled),
                EncryptionManager =  encryptionManager,
                ConnectionTimeout = connectionTimeout,
                SocketKeepAliveEnabled = socketKeepAlive,
                Ipv6Enabled = ipv6Enabled
            };
        }
    }
    internal class SocketSettings
    {
        public IHostResolver HostResolver { get; set; }
        public EncryptionManager EncryptionManager { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public bool SocketKeepAliveEnabled { get; set; }
        public bool Ipv6Enabled { get; set; }
    }
}
