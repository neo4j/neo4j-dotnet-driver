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
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Auth;

namespace Neo4j.Driver.Internal;

internal sealed class DriverContext
{
    internal DriverContext(
        Uri initialUri,
        IAuthTokenManager authTokenManager,
        Config config,
        IHostResolver customHostResolver = null)
    {
        InitialUri = initialUri;
        RoutingContext = Neo4jUri.ParseRoutingContext(initialUri, Neo4jUri.DefaultBoltPort);
        AuthTokenManager = authTokenManager;
        Config = config;
        EncryptionManager = EncryptionManager.Create(
            initialUri,
            config.NullableEncryptionLevel,
            config.TrustManager,
            config.Logger);
        DriverBookmarkManager = new DefaultBookmarkManager(new BookmarkManagerConfig());
        
        HostResolver = customHostResolver ??
            (RuntimeHelper.IsDotNetCore
                ? new SystemNetCoreHostResolver(new SystemHostResolver())
                : new DefaultHostResolver(
                    new SystemHostResolver(),
                    config.Ipv6Enabled));

        Metrics = config.MetricsEnabled ? new DefaultMetrics() : null;
    }

    public DefaultBookmarkManager DriverBookmarkManager { get; }

    /// <summary>
    /// The root uri configured on the driver.
    /// This is not a uri for a connection.
    /// </summary>
    public Uri InitialUri { get; }
    public Config Config { get; }
    /// <summary>
    /// Shortcut to Config.Logger.
    /// </summary>
    public ILogger Logger => Config.Logger;
    public IAuthTokenManager AuthTokenManager { get; }
    public EncryptionManager EncryptionManager { get; }
    public IHostResolver HostResolver { get; }
    public IInternalMetrics Metrics { get; }
    public IDictionary<string, string> RoutingContext { get; }

}
