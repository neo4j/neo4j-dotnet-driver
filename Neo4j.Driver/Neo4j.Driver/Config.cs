// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Metrics;

namespace Neo4j.Driver
{
    /// <summary>
    /// Use this class to configure the <see cref="IDriver"/>.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// This const defines the value of infinite interval in terms of configuration properties.
        /// </summary>
        public static readonly TimeSpan InfiniteInterval = TimeSpan.FromMilliseconds(-1);

        /// <summary>
        /// This const defines the value of infinite in terms of configuration properties.
        /// </summary>
        public const int Infinite = -1;

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
        /// <item><see cref="TrustManager"/> : <c><see cref="TrustManager"/>CreateChainTrust()</c> </item>
        /// <item><see cref="ConnectionTimeout"/>: <c>30s</c> </item>
        /// <item><see cref="SocketKeepAlive"/>: <c>true</c></item>
        /// <item><see cref="Ipv6Enabled"/>: <c>true</c></item>
        /// <br></br>
        /// <item><see cref="MaxConnectionPoolSize"/> : <c>500</c> </item>
        /// <item><see cref="ConnectionAcquisitionTimeout"/> : <c>1mins</c> </item>
        /// <item><see cref="ConnectionIdleTimeout"/>: <see cref="InfiniteInterval"/></item>
        /// <item><see cref="MaxConnectionLifetime"/>: <c>1h</c></item>
        /// <br></br>
        /// <item><see cref="DriverLogger"/> : <c>logs nothing.</c></item>
        /// <item><see cref="MaxTransactionRetryTime"/>: <c>30s</c></item>
        /// <br></br>
        /// <item><see cref="DefaultReadBufferSize"/> : <c>32K</c> </item>
        /// <item><see cref="MaxReadBufferSize"/> : <c>128K</c> </item>
        /// <item><see cref="DefaultWriteBufferSize"/> : <c>16K</c> </item>
        /// <item><see cref="MaxWriteBufferSize"/> : <c>64K</c> </item>
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
        public EncryptionLevel EncryptionLevel { get; set; } = EncryptionLevel.None;

        /// <summary>
        /// Gets or sets which <see cref="TrustManager"/> implementation should be used while establishing trust via TLS.
        /// </summary>
        public TrustManager TrustManager { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IDriverLogger"/> instance to be used to receive all logs produced by this driver.
        /// </summary>
        public IDriverLogger DriverLogger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Gets or sets the maximum transaction retry timeout.
        /// </summary>
        public TimeSpan MaxTransactionRetryTime { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the max idle connection pool size. If the value of this is not set,
        /// then it will default to be the same as <see cref="MaxConnectionPoolSize"/>
        /// </summary>
        /// <remarks>
        /// The max idle connection pool size represents the maximum number of idle connections buffered by the driver.
        /// An idle connection is a connection that has already been connected to the database instance and doesn't need to re-initialize.
        /// Setting this value to <see cref="Infinite"/> results in the idle pool size to be assigned the same value as <see cref="MaxConnectionPoolSize"/>.
        /// </remarks>
        /// <remarks>Also see <see cref="MaxConnectionPoolSize"/></remarks>
        public int MaxIdleConnectionPoolSize
        {
            get => _maxIdleConnPoolSize == Infinite ? MaxConnectionPoolSize : _maxIdleConnPoolSize;
            set => _maxIdleConnPoolSize = value;
        }

        private int _maxIdleConnPoolSize = Infinite;

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        /// <remarks>
        /// The max connection pool size specifies the allowed maximum number of idle and current in-use connections by the driver.
        /// a.k.a. ConnectionPoolSize = IdleConnectionPoolSize + InUseConnectionSize.
        /// When a driver reaches its allowed maximum connection pool size, no new connections can be established.
        /// Instead all threads that require a new connection have to wait until a connection is available to reclaim.
        /// See <see cref="ConnectionAcquisitionTimeout"/>for the maximum waiting time to acquire an idle connection from the pool.
        /// Setting this value to <see cref="Infinite"/> will result in an infinite pool.
        /// </remarks>
        /// <remarks>Also see <see cref="MaxIdleConnectionPoolSize"/></remarks>
        public int MaxConnectionPoolSize { get; set; } = 500;

        /// <summary>
        /// Gets or sets the maximum waiting time to either acquire an idle connection from the pool when connection pool is full
        /// or create a new connection when pool is not full.
        /// </summary>
        public TimeSpan ConnectionAcquisitionTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the connection timeout when establishing a connection with a server.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the socket keep alive option.
        /// </summary>
        public bool SocketKeepAlive { get; set; } = true;

        /// <summary>
        /// Gets or sets the idle timeout on pooled connections.
        /// A connection that has been idled in connection pool for longer than the given timeout is stale and will be closed once it is seen.
        /// Use <see cref="InfiniteInterval"/> to disable idle time checking.
        /// </summary>
        public TimeSpan ConnectionIdleTimeout { get; set; } = InfiniteInterval;

        /// <summary>
        /// Gets or sets the maximum connection lifetime on pooled connecitons.
        /// A connection that has been created for longer than the given time will be closed once it is seen.
        /// Use <see cref="InfiniteInterval"/> to disable connection lifetime checking.
        /// </summary>
        public TimeSpan MaxConnectionLifetime { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the connections to support ipv6 addresses.
        /// </summary>
        public bool Ipv6Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets a custom server address resolver used by the routing driver to resolve the initial address used to create the driver.
        /// Such resolution happens: 1) during the very first rediscovery when driver is created.
        /// 2) when all the known routers from the current routing table have failed and driver needs to fallback to the initial address.
        /// </summary>
        public IServerAddressResolver Resolver { get; set; } = new PassThroughServerAddressResolver();

        /// <summary>
        /// Gets or sets the metrics factory implementation to enable driver level metrics.
        /// Internally used for testing and experimenting.
        /// </summary>
        internal IMetricsFactory MetricsFactory { get; set; }

        /// <summary>
        /// Gets or sets the default read buffer size which the driver allocates for its internal buffers.
        /// </summary>
        public int DefaultReadBufferSize { get; set; } = Constants.DefaultReadBufferSize;

        /// <summary>
        /// Gets or sets the size when internal read buffers reach, will be released for garbage collection. 
        /// If reading large records (nodes, relationships or paths) and experiencing too much garbage collection consider increasing this size
        /// to a reasonable amount depending on your data.
        /// </summary>
        public int MaxReadBufferSize { get; set; } = Constants.MaxReadBufferSize;

        /// <summary>
        /// Gets or sets the default write buffer size which the driver allocates for its internal buffers.
        /// </summary>
        public int DefaultWriteBufferSize { get; set; } = Constants.DefaultWriteBufferSize;

        /// <summary>
        /// Gets or sets the size when internal write buffers reach, will be released for garbage collection. 
        /// If writing large values and experiencing too much garbage collection consider increasing this size
        /// to a reasonable amount depending on your data.
        /// </summary>
        public int MaxWriteBufferSize { get; set; } = Constants.MaxWriteBufferSize;

        private long _fetchSize = Constants.DefaultFetchSize;
        /// <summary>
        /// Gets or sets the default fetch size.
        /// Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from server in batches.
        /// This fetch size defines how many records to pull in each batch.
        /// Use <see cref="Infinite"/> to disable batching and always pull all records in one batch instead.
        /// </summary>
        public long FetchSize
        {
            get => _fetchSize;
            set => _fetchSize = FetchSizeUtil.AssertValidFetchSize(value);
        }

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

            public IConfigBuilder WithTrustManager(TrustManager manager)
            {
                _config.TrustManager = manager;
                return this;
            }

            public IConfigBuilder WithDriverLogger(IDriverLogger logger)
            {
                _config.DriverLogger = logger;
                return this;
            }

            public IConfigBuilder WithMaxIdleSessionPoolSize(int size)
            {
                return WithMaxIdleConnectionPoolSize(size);
            }

            public IConfigBuilder WithMaxIdleConnectionPoolSize(int size)
            {
                _config.MaxIdleConnectionPoolSize = size;
                return this;
            }

            public IConfigBuilder WithMaxConnectionPoolSize(int size)
            {
                _config.MaxConnectionPoolSize = size;
                return this;
            }

            public IConfigBuilder WithConnectionAcquisitionTimeout(TimeSpan timeSpan)
            {
                _config.ConnectionAcquisitionTimeout = timeSpan;
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

            public IConfigBuilder WithMaxConnectionLifetime(TimeSpan timeSpan)
            {
                _config.MaxConnectionLifetime = timeSpan;
                return this;
            }

            public IConfigBuilder WithIpv6Enabled(bool enable)
            {
                _config.Ipv6Enabled = enable;
                return this;
            }

            public IConfigBuilder WithResolver(IServerAddressResolver resolver)
            {
                Throw.ArgumentNullException.IfNull(resolver, nameof(resolver));
                _config.Resolver = resolver;
                return this;
            }

            public IConfigBuilder WithDefaultReadBufferSize(int defaultReadBufferSize)
            {
                _config.DefaultReadBufferSize = defaultReadBufferSize;
                return this;
            }

            public IConfigBuilder WithMaxReadBufferSize(int maxReadBufferSize)
            {
                _config.MaxReadBufferSize = maxReadBufferSize;
                return this;
            }

            public IConfigBuilder WithDefaultWriteBufferSize(int defaultWriteBufferSize)
            {
                _config.DefaultWriteBufferSize = defaultWriteBufferSize;
                return this;
            }

            public IConfigBuilder WithMaxWriteBufferSize(int maxWriteBufferSize)
            {
                _config.MaxWriteBufferSize = maxWriteBufferSize;
                return this;
            }

            public IConfigBuilder WithFetchSize(long size)
            {
                _config.FetchSize = size;
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
        /// Sets the <see cref="TrustManager"/> to use while establishing trust via TLS.
        /// The <paramref name="manager"/> will not take effect if <see cref="Config.EncryptionLevel"/> decides to use no TLS
        /// encryption on the connections.
        /// </summary>
        /// <param name="manager">A <see cref="TrustManager"/> instance.</param>
        /// <returns></returns>
        IConfigBuilder WithTrustManager(TrustManager manager);

        /// <summary>
        /// Sets the <see cref="Config"/> to use a given <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="IDriverLogger"/> instance to use, if <c>null</c> no logging will occur.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithDriverLogger(IDriverLogger logger);

        /// <summary>
        /// Sets the size of the idle connection pool.
        /// </summary>
        /// <param name="size">The size of the <see cref="Config.MaxIdleConnectionPoolSize"/>,
        /// set to 0 will disable connection pooling.</param>.
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithMaxIdleConnectionPoolSize(int size);

        /// <summary>
        /// Sets the size of the connection pool.
        /// </summary>
        /// <param name="size">The size of the <see cref="Config.MaxConnectionPoolSize"/></param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithMaxConnectionPoolSize(int size);

        /// <summary>
        /// Sets the maximum connection acquisition timeout for waiting for a connection to become available in idle connection pool
        /// when <see cref="Config.MaxConnectionPoolSize"/> is reached.
        /// </summary>
        /// <param name="timeSpan">The connection acquisition timeout.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithConnectionAcquisitionTimeout(TimeSpan timeSpan);

        /// <summary>
        /// Specify socket connection timeout.
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, or <see cref="Config.InfiniteInterval"/> to wait indefinitely.
        /// </summary>
        /// <param name="timeSpan">Represents the number of milliseconds to wait or <see cref="Config.InfiniteInterval"/> to wait indefinitely.</param>
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
        /// Specify the maximum time transactions are allowed to retry via transaction functions.
        /// 
        /// These methods will retry the given unit of work on <see cref="SessionExpiredException"/>,
        /// <see cref="TransientException"/> and <see cref="ServiceUnavailableException"/>
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

        /// <summary>
        /// Specify the maximum connection life time.
        /// The connection that has been created for longer than specified time will not be reused but closed.
        /// </summary>
        /// <param name="timeSpan">The max timespan that a connection can be reused after has been created for.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithMaxConnectionLifetime(TimeSpan timeSpan);

        /// <summary>
        /// Setting this option to true will enable ipv6 on socket connections.
        /// </summary>
        /// <param name="enable">true to enable ipv6, false to only support ipv4 addresses.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        IConfigBuilder WithIpv6Enabled(bool enable);

        /// <summary>
        /// Gets or sets a custom server address resolver used by the routing driver to resolve the initial address used to create the driver.
        /// Such resolution happens: 1) during the very first rediscovery when driver is created.
        /// 2) when all the known routers from the current routing table have failed and driver needs to fallback to the initial address.
        /// </summary>
        /// <param name="resolver">The resolver, default to a resolver that simply pass the initial server address as it is.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        IConfigBuilder WithResolver(IServerAddressResolver resolver);

        /// <summary>
        /// Specify the default read buffer size which the driver allocates for its internal buffers.
        /// </summary>
        /// <param name="defaultReadBufferSize">the buffer size</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        IConfigBuilder WithDefaultReadBufferSize(int defaultReadBufferSize);

        /// <summary>
        /// Specify the size when internal read buffers reach, will be released for garbage collection. 
        /// </summary>
        /// <param name="maxReadBufferSize">the buffer size</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>If reading large records (nodes, relationships or paths) and experiencing too much garbage collection 
        /// consider increasing this size to a reasonable amount depending on your data.</remarks>
        IConfigBuilder WithMaxReadBufferSize(int maxReadBufferSize);

        /// <summary>
        /// Specify the default write buffer size which the driver allocates for its internal buffers.
        /// </summary>
        /// <param name="defaultWriteBufferSize">the buffer size</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        IConfigBuilder WithDefaultWriteBufferSize(int defaultWriteBufferSize);

        /// <summary>
        /// Specify the size when internal write buffers reach, will be released for garbage collection. 
        /// </summary>
        /// <param name="maxWriteBufferSize">the buffer size</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>If writing large values and experiencing too much garbage collection 
        /// consider increasing this size to a reasonable amount depending on your data.</remarks>
        IConfigBuilder WithMaxWriteBufferSize(int maxWriteBufferSize);


        /// <summary>
        /// Sets the default fetch size.
        /// Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from server in batches.
        /// This fetch size defines how many records to pull in each batch.
        /// Use -1 to disable batching and always pull all records in one go instead.
        /// </summary>
        /// <param name="size">The fetch size.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        IConfigBuilder WithFetchSize(long size);
    }
}