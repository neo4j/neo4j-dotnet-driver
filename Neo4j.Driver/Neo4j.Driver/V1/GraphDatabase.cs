// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.Collections.Generic;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Routing;

namespace Neo4j.Driver.V1
{
    /// <summary>
    ///     Creates <see cref="IDriver" /> instances, optionally letting you
    ///     configure them.
    /// </summary>
    public static class GraphDatabase
    {
        internal const int DefaultBoltPort = 7687;

        /// <summary>
        ///     Returns a driver for a Neo4j instance with default configuration settings.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <returns>A new <see cref="IDriver" /> instance specified by the <paramref name="uri" />.</returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
        public static IDriver Driver(string uri)
        {
            return Driver(new Uri(uri));
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with default configuration settings.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <returns>A new <see cref="IDriver" /> instance specified by the <paramref name="uri" />.</returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
        public static IDriver Driver(Uri uri)
        {
            return Driver(uri, (Config)null);
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with custom configuration.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <param name="config">
        ///     Configuration for the driver instance to use, if <c>null</c> <see cref="Config.DefaultConfig" />
        ///     is used.
        /// </param>
        /// <returns>A new <see cref="IDriver" /> instance specified by the <paramref name="uri" />.</returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
        public static IDriver Driver(string uri, Config config)
        {
            return Driver(new Uri(uri), config);
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with custom configuration.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <param name="config">
        ///     Configuration for the driver instance to use, if <c>null</c> <see cref="Config.DefaultConfig" />
        ///     is used.
        /// </param>
        /// <returns>A new <see cref="IDriver" /> instance specified by the <paramref name="uri" />.</returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
        public static IDriver Driver(Uri uri, Config config)
        {
            return Driver(uri, AuthTokens.None, config);
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with default configuration settings.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens" />.</param>
        /// <returns>A new <see cref="IDriver" /> instance specified by the <paramref name="uri" />.</returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
        public static IDriver Driver(string uri, IAuthToken authToken)
        {
            return Driver(new Uri(uri), authToken);
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with default configuration settings.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens" />.</param>
        /// <returns>A new <see cref="IDriver" /> instance specified by the <paramref name="uri" />.</returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c>.</exception>
        public static IDriver Driver(Uri uri, IAuthToken authToken)
        {
            return Driver(uri, authToken, null);
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with custom configuration.
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>.
        ///     If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        ///     The supported protocols in URI could either be <c>bolt</c> or <c>bolt+routing</c>.
        ///     The protocol <c>bolt</c> should be used when creating a driver connecting to the Neo4j instance directly.
        ///     The protocol <c>bolt+routing</c> should be used when creating a driver with built-in routing.
        /// </param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens" />.</param>
        /// <param name="config">
        ///     Configuration for the driver instance to use, if <c>null</c> <see cref="Config.DefaultConfig" />
        ///     is used.
        /// </param>
        /// <returns>A new driver to the database instance specified by the <paramref name="uri"/>.</returns>
        public static IDriver Driver(string uri, IAuthToken authToken, Config config)
        {
            return Driver(new Uri(uri), authToken, config);
        }

        /// <summary>
        ///     Returns a driver for a Neo4j instance with custom configuration.
        /// </summary>
        /// <param name="uri">        
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>bolt://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.</param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens" />.</param>
        /// <param name="config">
        ///     Configuration for the driver instance to use, if <c>null</c> <see cref="Config.DefaultConfig" />
        ///     is used.
        /// </param>
        /// <returns>A new driver to the database instance specified by the <paramref name="uri"/>.</returns>
        public static IDriver Driver(Uri uri, IAuthToken authToken, Config config)
        {
            Throw.ArgumentNullException.IfNull(uri, nameof(uri));
            Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
            config = config ?? Config.DefaultConfig;

            var logger = config.Logger;

            var parsedUri = uri.ParseBoltUri(DefaultBoltPort);
            var routingContext = uri.ParseRoutingContext();

            var routingSettings = new RoutingSettings(parsedUri, routingContext);
            var connectionPoolSettings = new ConnectionPoolSettings(config);

            var connectionSettings = new ConnectionSettings(authToken, config);
            var bufferSettings = new BufferSettings(config);
            var connectionFactory = new PooledConnectionFactory(connectionSettings, bufferSettings, logger);

            var retryLogic = new ExponentialBackoffRetryLogic(config.MaxTransactionRetryTime, logger);

            IConnectionProvider connectionProvider = null;
            switch (parsedUri.Scheme.ToLower())
            {
                case "bolt":
                    EnsureNoRoutingContext(uri, routingContext);
                    connectionProvider = new ConnectionPool(parsedUri, connectionFactory, connectionPoolSettings, logger);
                    break;
                case "bolt+routing":
                    connectionProvider = new LoadBalancer(routingSettings, connectionPoolSettings, connectionFactory, config);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported URI scheme: {parsedUri.Scheme}");
            }

            return new Internal.Driver(parsedUri, connectionProvider, retryLogic, logger, connectionPoolSettings.DriverMetrics);
        }

        private static void EnsureNoRoutingContext(Uri uri, IDictionary<string, string> routingContext)
        {
            if (routingContext.Count != 0)
            {
                throw new ArgumentException($"Routing context are not supported with scheme 'bolt'. Given URI: '{uri}'");
            }
        }
    }
}
