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
using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Control the level of encryption to require.
    /// </summary>
    public enum EncryptionLevel
    {
        /// <summary>
        /// No encryption at all.
        /// </summary>
        None,
        /// <summary>
        /// Always encrypted.
        /// </summary>
        Encrypted
    }

    /// <summary>
    /// Specify the trust strategy the driver adopts to trust a server.
    /// </summary>
    public enum TrustStrategy
    {
        /// <summary>
        /// Trust all servers.
        /// </summary>
        TrustAllCertificates,
        /// <summary>
        /// Trust the servers whoes certifiacte is trusted by OS.
        /// </summary>
        TrustSystemCaSignedCertificates
    }

    /// <summary>
    /// Use this class to config the <see cref="IDriver"/>.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// When the <see cref="MaxIdleSessionPoolSize"/> is set to <see cref="InfiniteMaxIdleSessionPoolSize" />, the idle session pool will pool all sessions created by the driver.
        /// </summary>
        public const int InfiniteMaxIdleSessionPoolSize = -1;
        static Config()
        {
            DefaultConfig = new Config();
        }

        /// <summary>
        /// Returns the default configuration for the <see cref="IDriver"/>.
        /// </summary>
        /// <remarks>
        /// The defaults are <br/>
        /// <list type="bullet">
        /// <item><see cref="EncryptionLevel"/> : <c><see cref="EncryptionLevel"/> Encrypted</c> </item>
        /// <item><see cref="TrustStrategy"/> : <c><see cref="TrustStrategy"/>TrustAllCertificates</c> </item>
        /// <item><see cref="Logger"/> : <c>DebugLogger</c> at <c><see cref="LogLevel"/> Info</c> </item>
        /// <item><see cref="MaxIdleSessionPoolSize"/> : <c>10</c> </item>
        /// <item><see cref="ConnectionTimeout"/>: <c>5s</c> </item>
        /// <item><see cref="SocketKeepAlive"/>: <c>true</c></item>
        /// <item><see cref="MaxTransactionRetryTime"/>: <c>30s</c></item>
        /// <item><see cref="ConnectionIdleTimeout"/>: <c>Infinite(-1ms)</c></item>
        /// </list>
        /// </remarks>
        public static Config DefaultConfig { get; }

        /// <summary>
        /// Create an instance of <see cref="IConfigBuilder"/> to build a <see cref="Config"/>.
        /// </summary>
        public static IConfigBuilder Builder => new ConfigBuilder(new Config());

        /// <summary>
        /// Gets or sets the use of encryption for all the connections created by the <see cref="IDriver"/>.
        /// </summary>
        public EncryptionLevel EncryptionLevel { get; set; } = EncryptionLevel.Encrypted;

        /// <summary>
        /// Gets or sets how to determine the authenticity of an encryption certificate provided by the Neo4j instance we are connecting to.
        /// </summary>
        public TrustStrategy TrustStrategy { get; set; } = TrustStrategy.TrustAllCertificates;

        /// <summary>
        /// Gets or sets the <see cref="ILogger"/> instance to be used by the <see cref="ISession"/>s.
        /// </summary>
        public ILogger Logger { get; set; } = new DebugLogger {Level = LogLevel.Info};

        /// <summary>
        /// Gets or sets the maximum transaction rety time.
        /// </summary>
        public TimeSpan MaxTransactionRetryTime { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the max idle session pool size.
        /// </summary>
        /// <remarks> 
        /// The max idle session pool size represents the maximum number of sessions buffered by the driver. 
        /// A buffered <see cref="ISession"/> is a session that has already been connected to the database instance and doesn't need to re-initialize.
        /// </remarks>
        public int MaxIdleSessionPoolSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the connection timeout when establishing a connection with a server.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the socket keep alive option.
        /// </summary>
        public bool SocketKeepAlive { get; set; } = true;

        internal static readonly TimeSpan Infinite = TimeSpan.FromMilliseconds(-1);

        /// <summary>
        /// Gets or sets the idle timeout on pooled connecitons.
        /// A connection that has been idled in connection pool for longer than the given timeout is stale and will be closed once it is seen.
        /// Any negative <see cref="TimeSpan"/> value will be considered to be "Infinite",
        /// a.k.a. pooled connections will never be stale.
        /// </summary>
        public TimeSpan ConnectionIdleTimeout { get; set; } = Infinite;

        /// <summary>
        /// Gets or sets the statistics collector to which the statistics inside the driver could be published.
        /// </summary>
        internal IStatisticsCollector DriverStatisticsCollector { get; set; }

        private class ConfigBuilder : IConfigBuilder
        {
            private readonly Config _config;

            internal ConfigBuilder(Config config)
            {
                _config = config;
            }

            public IConfigBuilder WithEncryptionLevel(EncryptionLevel level)
            {
                _config.EncryptionLevel = level;
                return this;
            }

            public IConfigBuilder WithTrustStrategy(TrustStrategy strategy)
            {
                _config.TrustStrategy = strategy;
                return this;
            }

            public IConfigBuilder WithLogger(ILogger logger)
            {
                _config.Logger = logger;
                return this;
            }

            public IConfigBuilder WithMaxIdleSessionPoolSize(int size)
            {
                _config.MaxIdleSessionPoolSize = size;
                return this;
            }

            public IConfigBuilder WithConnectionTimeout(TimeSpan timeSpan)
            {
                _config.ConnectionTimeout = timeSpan;
                return this;
            }

            public Config ToConfig()
            {
                return _config;
            }

            public IConfigBuilder WithSocketKeepAliveEnabled(bool enable)
            {
                _config.SocketKeepAlive = enable;
                return this;
            }

            public IConfigBuilder WithMaxTransactionRetryTime(TimeSpan time)
            {
                _config.MaxTransactionRetryTime = time;
                return this;
            }

            public IConfigBuilder WithConnectionIdleTimeout(TimeSpan timeSpan)
            {
                _config.ConnectionIdleTimeout = timeSpan;
                return this;
            }
        }
    }
    /// <summary>
    /// Provides a way to generate a <see cref="Config"/> instance fluently.
    /// </summary>
    public interface IConfigBuilder
    {
        /// <summary>
        /// Builds the <see cref="Config"/> instance based on the previously set values.
        /// </summary>
        /// <remarks>>
        /// If no value was set for a property the defaults specified in <see cref="Config.DefaultConfig"/> will be used.
        /// </remarks>
        /// <returns>A <see cref="Config"/> instance.</returns>
        Config ToConfig();
        
        /// <summary>
        /// Sets the <see cref="Config"/> to use TLS if <paramref name="level"/> is <c>true</c>.
        /// </summary>
        /// <param name="level"><see cref="EncryptionLevel.Encrypted"/> enables TLS for the connection, <see cref="EncryptionLevel.None"/> otherwise. See <see cref="EncryptionLevel"/> for more info</param>.
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithEncryptionLevel(EncryptionLevel level);

        /// <summary>
        /// Sets <see cref="Config"/> to use the verification <paramref name="strategy"/> when establishing TLS connections.
        /// The <paramref name="strategy"/> will not take effects if <see cref="Config.EncryptionLevel"/> decides to use no TLS encryption on the connections.
        /// </summary>
        /// <param name="strategy">See <see cref="TrustStrategy"/> for available strategies.</param>
        /// <returns></returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithTrustStrategy(TrustStrategy strategy);

        /// <summary>
        /// Sets the <see cref="Config"/> to use a given <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use, if <c>null</c> no logging will occur.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithLogger(ILogger logger);
        
        /// <summary>
        /// Sets the size of the idle session pool.
        /// </summary>
        /// <param name="size">The size of the <see cref="Config.MaxIdleSessionPoolSize"/>, set to <see cref="Config.InfiniteMaxIdleSessionPoolSize"/> to pool all sessions.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithMaxIdleSessionPoolSize(int size);

        /// <summary>
        /// Specify socket connection timeout.
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </summary>
        /// <param name="timeSpan">Represents the number of milliseconds to wait or -1 ms to wait indefinitely.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithConnectionTimeout(TimeSpan timeSpan);

        /// <summary>
        /// Enable socket to send keep alive pings on TCP level to prevent pooled socket connections from getting killed after leaving client idle for a long time.
        /// The interval of keep alive pings are set via your OS system.
        /// </summary>
        /// <param name="enable"></param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithSocketKeepAliveEnabled(bool enable);

        /// <summary>
        /// Specify the maximum time transactions are allowed to retry via <see cref="ISession.ReadTransaction"/> and <see cref="ISession.WriteTransaction"/>.
        /// These methods will retry the given unit of work on <see cref="SessionExpiredException"/>, <see cref="TransientException"/> and <see cref="ServiceUnavailableException"/>
        /// with exponential backoff using initial delay of 1 second.
        /// Default value is 30 seconds.
        /// </summary>
        /// <param name="time">Specify the maximum retry time. </param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithMaxTransactionRetryTime(TimeSpan time);

        /// <summary>
        /// Specify the connection idle timeout.
        /// The connection that has been idled in pool for longer than specified timeout will not be reused but closed.
        /// </summary>
        /// <param name="timeSpan">The max timespan that a connection can be reused after has been idle for.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithConnectionIdleTimeout(TimeSpan timeSpan);
    }
}