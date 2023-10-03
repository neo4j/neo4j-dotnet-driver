// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using Neo4j.Driver.Preview.Auth;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Metrics;

namespace Neo4j.Driver.Internal;

internal sealed class ConnectionSettings
{
    internal ConnectionSettings(
        Uri rootUri,
        IAuthTokenManager authTokenManager,
        Config config)
    {
        AuthTokenManager = authTokenManager;
        DriverConfig = config;
        EncryptionManager = EncryptionManager.Create(
            rootUri,
            config.NullableEncryptionLevel,
            config.TrustManager,
            config.Logger);
        HostResolver = RuntimeHelper.IsDotNetCore
            ? new SystemNetCoreHostResolver(new SystemHostResolver())
            : new DefaultHostResolver(
                new SystemHostResolver(),
                config.Ipv6Enabled);

        var metrics = config.MetricsEnabled ? new DefaultMetrics() : null;
        PoolSettings = new ConnectionPoolSettings(config, metrics);
    }
    
    public void WithTestResolver(IHostResolver resolver)
    {
        // in testing allow the host resolver to be overridden
        HostResolver = resolver;
    }

    public Config DriverConfig { get; }
    public ConnectionPoolSettings PoolSettings { get; }
    public IAuthTokenManager AuthTokenManager { get; }
    public string UserAgent => DriverConfig.UserAgent;
    public EncryptionManager EncryptionManager { get; set; }
    public IHostResolver HostResolver { get; private set; }
    public TimeSpan ConnectionTimeout => DriverConfig.ConnectionTimeout;
    public bool SocketKeepAliveEnabled => DriverConfig.SocketKeepAlive;
    public bool Ipv6Enabled => DriverConfig.Ipv6Enabled;
}
