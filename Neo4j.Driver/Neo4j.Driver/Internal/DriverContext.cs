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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Preview.Auth;

namespace Neo4j.Driver.Internal;

internal sealed class DriverContext
{
    internal DriverContext(
        Uri rootUri,
        IAuthTokenManager authTokenManager,
        Config config,
        IHostResolver customHostResolver = null)
    {
        RootUri = rootUri;
        AuthTokenManager = authTokenManager;
        Config = config;
        EncryptionManager = EncryptionManager.Create(
            rootUri,
            config.NullableEncryptionLevel,
            config.TrustManager,
            config.Logger);

        HostResolver = customHostResolver ??
            (RuntimeHelper.IsDotNetCore
                ? new SystemNetCoreHostResolver(new SystemHostResolver())
                : new DefaultHostResolver(
                    new SystemHostResolver(),
                    config.Ipv6Enabled));

        Metrics = config.MetricsEnabled ? new DefaultMetrics() : null;
    }
    
    /// <summary>
    /// The root uri configured on the driver.
    /// This is not a uri for a connection.
    /// </summary>
    public Uri RootUri { get; }
    public Config Config { get; }
    public IAuthTokenManager AuthTokenManager { get; }
    public EncryptionManager EncryptionManager { get; }
    public IHostResolver HostResolver { get; }
    public IInternalMetrics Metrics { get; }
}
