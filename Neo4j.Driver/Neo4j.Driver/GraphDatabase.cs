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
using Neo4j.Driver.Auth;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver;

/// <summary>Creates <see cref="IDriver"/> instances, optionally letting you configure them.</summary>
public static class GraphDatabase
{
    internal const int DefaultBoltPort = 7687;

    /// <summary>Returns a driver for a Neo4j instance with default configuration settings.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <returns>A new <see cref="IDriver"/> instance specified by the <paramref name="uri"/>.</returns>
    /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/>.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
    public static IDriver Driver(string uri)
    {
        return Driver(new Uri(uri));
    }

    /// <summary>Returns a driver for a Neo4j instance with default configuration settings.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <returns>A new <see cref="IDriver"/> instance specified by the <paramref name="uri"/>.</returns>
    /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/>.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
    public static IDriver Driver(Uri uri)
    {
        return Driver(uri, (Action<ConfigBuilder>)null);
    }

    /// <summary>Returns a driver for a Neo4j instance with custom configuration.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="action">
    /// Specifies how to build a driver configuration <see cref="Config"/>, using
    /// <see cref="ConfigBuilder"/>. If set to <c>null</c>, then no modification will be carried out and the default driver
    /// configurations <see cref="Config"/> will be used when creating the driver.
    /// </param>
    /// <returns>A new <see cref="IDriver"/> instance specified by the <paramref name="uri"/>.</returns>
    /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/>.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
    public static IDriver Driver(string uri, Action<ConfigBuilder> action)
    {
        return Driver(new Uri(uri), action);
    }

    /// <summary>Returns a driver for a Neo4j instance with custom configuration.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="action">
    /// Specifies how to build a driver configuration <see cref="Config"/>, using
    /// <see cref="ConfigBuilder"/>. If set to <c>null</c>, then no modification will be carried out and the default driver
    /// configurations <see cref="Config"/> will be used when creating the driver.
    /// </param>
    /// <returns>A new <see cref="IDriver"/> instance specified by the <paramref name="uri"/>.</returns>
    /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/>.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
    public static IDriver Driver(Uri uri, Action<ConfigBuilder> action)
    {
        return Driver(uri, AuthTokens.None, action);
    }

    /// <summary>Returns a driver for a Neo4j instance with default configuration settings.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="authToken">Authentication to use, <see cref="AuthTokens"/>.</param>
    /// <returns>A new <see cref="IDriver"/> instance specified by the <paramref name="uri"/>.</returns>
    /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/>.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
    public static IDriver Driver(string uri, IAuthToken authToken)
    {
        return Driver(new Uri(uri), authToken);
    }

    internal static IDriver Driver(string uri, IAuthTokenManager authTokenManager)
    {
        return Driver(new Uri(uri), authTokenManager);
    }

    /// <summary>Returns a driver for a Neo4j instance with default configuration settings.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="authToken">Authentication to use, <see cref="AuthTokens"/>.</param>
    /// <returns>A new <see cref="IDriver"/> instance specified by the <paramref name="uri"/>.</returns>
    /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/>.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
    public static IDriver Driver(Uri uri, IAuthToken authToken)
    {
        return Driver(uri, authToken, null);
    }

    internal static IDriver Driver(Uri uri, IAuthTokenManager authTokenManager)
    {
        return Driver(uri, authTokenManager, null);
    }

    /// <summary>Returns a driver for a Neo4j instance with custom configuration.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="authToken">Authentication to use, <see cref="AuthTokens"/>.</param>
    /// <param name="action">
    /// Specifies how to build a driver configuration <see cref="Config"/>, using
    /// <see cref="ConfigBuilder"/>. If set to <c>null</c>, then no modification will be carried out and the default driver
    /// configurations <see cref="Config"/> will be used when creating the driver.
    /// </param>
    /// <returns>A new driver to the database instance specified by the <paramref name="uri"/>.</returns>
    public static IDriver Driver(string uri, IAuthToken authToken, Action<ConfigBuilder> action)
    {
        return Driver(new Uri(uri), authToken, action);
    }

    internal static IDriver Driver(string uri, IAuthTokenManager authTokenManager, Action<ConfigBuilder> action)
    {
        return Driver(new Uri(uri), authTokenManager, action);
    }


    /// <summary>Returns a driver for a Neo4j instance with custom configuration.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>neo4j://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will be
    /// used.
    /// </param>
    /// <param name="authToken">Authentication to use, <see cref="AuthTokens"/>.</param>
    /// <param name="action">
    /// Defines how to build a driver configuration <see cref="Config"/> using <see cref="ConfigBuilder"/>
    /// . If set to <c>null</c>, then no modification will be carried out on the build. As a result, a default config with
    /// default settings will be used <see cref="Config"/> when creating the new driver.
    /// </param>
    /// <returns>A new driver to the database instance specified by the <paramref name="uri"/>.</returns>
    public static IDriver Driver(Uri uri, IAuthToken authToken, Action<ConfigBuilder> action)
    {
        return Driver(uri, AuthTokenManagers.Static(authToken), action);
    }

    internal static IDriver Driver(Uri uri, IAuthTokenManager authTokenManager, Action<ConfigBuilder> action)
    {
        uri = uri ?? throw new ArgumentNullException(nameof(uri));
        authTokenManager = authTokenManager ?? throw new ArgumentNullException(nameof(authTokenManager));

        var config = ConfigBuilders.BuildConfig(action);

        var connectionSettings = new ConnectionSettings(uri, authTokenManager, config);
        var bufferSettings = new BufferSettings(config);
        var connectionFactory = new PooledConnectionFactory(bufferSettings, config.Logger);

        return CreateDriver(uri, config, connectionFactory, connectionSettings);
    }

    /// <summary>
    /// Gets a new <see cref="IBookmarkManagerFactory"/>, which can construct a new default
    /// <see cref="IBookmarkManager"/> instance.<br/>
    /// The <see cref="IBookmarkManager"/> instance should be passed to <see cref="SessionConfigBuilder"/>
    /// when opening a new session with <see cref="SessionConfigBuilder.WithBookmarkManager"/>.
    /// </summary>
    public static IBookmarkManagerFactory BookmarkManagerFactory => new BookmarkManagerFactory();

    internal static IDriver CreateDriver(
        Uri uri,
        Config config,
        IPooledConnectionFactory connectionFactory,
        ConnectionSettings connectionSettings)
    {
        var logger = config.Logger;

        var parsedUri = uri.ParseBoltUri(DefaultBoltPort);
        var routingContext = uri.ParseRoutingContext(DefaultBoltPort);
        var routingSettings = new RoutingSettings(parsedUri, routingContext, config);

        var metrics = config.MetricsEnabled ? new DefaultMetrics() : null;
        var connectionPoolSettings = new ConnectionPoolSettings(config, metrics);

        var retryLogic = new AsyncRetryLogic(config.MaxTransactionRetryTime, logger);

        EnsureNoRoutingContextOnBolt(uri, routingContext);

        IConnectionProvider connectionProvider = parsedUri.IsRoutingUri()
            ? new LoadBalancer(
                connectionFactory,
                routingSettings,
                connectionPoolSettings,
                connectionSettings,
                logger,
                config.NotificationsConfig)
            : new ConnectionPool(
                    parsedUri,
                    connectionFactory,
                    connectionPoolSettings,
                    logger,
                    connectionSettings,
                    null,
                    config.NotificationsConfig);

        return new Internal.Driver(
            parsedUri,
            connectionSettings.SocketSettings.EncryptionManager.UseTls,
            connectionProvider,
            retryLogic,
            logger,
            metrics,
            config);
    }

    private static void EnsureNoRoutingContextOnBolt(Uri uri, IDictionary<string, string> routingContext)
    {
        if (!uri.IsRoutingUri() && !string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException($"Routing context are not supported with scheme 'bolt'. Given URI: '{uri}'");
        }
    }
}
