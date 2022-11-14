// Copyright (c) "Neo4j"
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
using System.Reflection;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal;

internal class ConnectionSettings
{
    public ConnectionSettings(Uri uri, IAuthToken auth, Config config)
        : this(
            auth,
            EncryptionManager.Create(
                uri,
                config.NullableEncryptionLevel,
                config.TrustManager,
                config.Logger),
            config.ConnectionTimeout,
            config.SocketKeepAlive,
            config.Ipv6Enabled,
            config.UserAgent)
    {
    }

    private ConnectionSettings(
        IAuthToken authToken,
        EncryptionManager encryptionManager,
        TimeSpan connectionTimeout,
        bool socketKeepAlive,
        bool ipv6Enabled,
        string userAgent)
    {
        AuthToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
        UserAgent = userAgent ?? DefaultUserAgent;

        IHostResolver systemResolver = new SystemHostResolver();
        if (RuntimeHelper.IsDotNetCore)
        {
            systemResolver = new SystemNetCoreHostResolver(systemResolver);
        }

        SocketSettings = new SocketSettings(
            new DefaultHostResolver(systemResolver, ipv6Enabled),
            encryptionManager)
        {
            ConnectionTimeout = connectionTimeout,
            SocketKeepAliveEnabled = socketKeepAlive,
            Ipv6Enabled = ipv6Enabled
        };
    }

    internal static string DefaultUserAgent
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"neo4j-dotnet/{version!.Major}.{version.Minor}";
        }
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
