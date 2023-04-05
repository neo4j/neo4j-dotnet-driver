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

namespace Neo4j.Driver.Internal;

internal class ConnectionSettings
{
    internal ConnectionSettings(Uri uri, IAuthToken authToken, Config config, IHostResolver hostResolver = null)
    {
        AuthToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
        UserAgent = config.UserAgent;
        var resolver = hostResolver switch
        {
            //TODO: Consider moving to a factory.
            null when RuntimeHelper.IsDotNetCore => new SystemNetCoreHostResolver(new SystemHostResolver()),
            null when !RuntimeHelper.IsDotNetCore => new DefaultHostResolver(
                new SystemHostResolver(),
                config.Ipv6Enabled),
            // test code can provide resolver.
            _ => hostResolver
        };

        var encryptionManager = EncryptionManager.Create(
            uri,
            config.NullableEncryptionLevel,
            config.TrustManager,
            config.Logger);

        SocketSettings = new SocketSettings(
            resolver,
            encryptionManager)
        {
            ConnectionTimeout = config.ConnectionTimeout,
            SocketKeepAliveEnabled = config.SocketKeepAlive,
            Ipv6Enabled = config.Ipv6Enabled
        };
    }

    public IAuthToken AuthToken { get; }
    public string UserAgent { get; }
    public SocketSettings SocketSettings { get; }
}

internal class SocketSettings
{
    public SocketSettings(IHostResolver hostResolver, EncryptionManager encryptionManager)
    {
        HostResolver = hostResolver ?? throw new ArgumentNullException(nameof(hostResolver));
        EncryptionManager = encryptionManager ?? throw new ArgumentNullException(nameof(encryptionManager));
    }

    public IHostResolver HostResolver { get; }
    public EncryptionManager EncryptionManager { get; }
    public TimeSpan ConnectionTimeout { get; init; }
    public bool SocketKeepAliveEnabled { get; init; }
    public bool Ipv6Enabled { get; init; }
}
