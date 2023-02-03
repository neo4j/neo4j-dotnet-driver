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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver;

/// <summary>Use this class to configure the <see cref="IDriver"/>.</summary>
/// <remarks>
/// The defaults for fields in this class are <br/>
/// <list type="bullet">
///     <item><see cref="EncryptionLevel"/> : <c><see cref="EncryptionLevel"/> Encrypted</c> </item>
///     <item><see cref="TrustManager"/> : <c><see cref="TrustManager"/>CreateChainTrust()</c> </item>
///     <item><see cref="ConnectionTimeout"/>: <c>30s</c> </item> <item><see cref="SocketKeepAlive"/>: <c>true</c></item>
///     <item><see cref="Ipv6Enabled"/>: <c>true</c></item> <br></br>
///     <item><see cref="MaxConnectionPoolSize"/> : <c>100</c> </item>
///     <item><see cref="ConnectionAcquisitionTimeout"/> : <c>1mins</c> </item>
///     <item><see cref="ConnectionIdleTimeout"/>: <see cref="InfiniteInterval"/></item>
///     <item><see cref="MaxConnectionLifetime"/>: <c>1h</c></item> <br></br>
///     <item><see cref="Logger"/> : <c>logs nothing.</c></item>
///     <item><see cref="MaxTransactionRetryTime"/>: <c>30s</c></item> <br></br>
///     <item><see cref="DefaultReadBufferSize"/> : <c>32K</c> </item>
///     <item><see cref="MaxReadBufferSize"/> : <c>128K</c> </item>
///     <item><see cref="DefaultWriteBufferSize"/> : <c>16K</c> </item>
///     <item><see cref="MaxWriteBufferSize"/> : <c>64K</c> </item>
/// </list>
/// </remarks>
public class Config
{
    /// <summary>This const defines the value of infinite in terms of configuration properties.</summary>
    public const int Infinite = -1;

    /// <summary>This const defines the value of infinite interval in terms of configuration properties.</summary>
    public static readonly TimeSpan InfiniteInterval = TimeSpan.FromMilliseconds(-1);

    /// <summary>Returns the default configuration for the <see cref="IDriver"/>.</summary>
    internal static readonly Config Default = new();

    private long _fetchSize = Constants.DefaultFetchSize;

    private int _maxIdleConnPoolSize = Infinite;

    /// <summary>Create an instance of <see cref="ConfigBuilder"/> to build a <see cref="Config"/>.</summary>
    internal static ConfigBuilder Builder => new(new Config());

    /// <summary>The use of encryption for all the connections created by the <see cref="IDriver"/>.</summary>
    public EncryptionLevel EncryptionLevel
    {
        get => NullableEncryptionLevel.GetValueOrDefault(EncryptionLevel.None);
        internal set => NullableEncryptionLevel = value;
    }

    internal EncryptionLevel? NullableEncryptionLevel { get; set; }

    /// <summary>Specifies which <see cref="TrustManager"/> implementation should be used while establishing trust via TLS.</summary>
    public TrustManager TrustManager { get; internal set; }

    /// <summary>The <see cref="ILogger"/> instance to be used to receive all logs produced by this driver.</summary>
    public ILogger Logger { get; internal set; } = NullLogger.Instance;

    /// <summary>The maximum transaction retry timeout.</summary>
    public TimeSpan MaxTransactionRetryTime { get; internal set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The max idle connection pool size. If the value of this is not internal set, then it will default to be the
    /// same as <see cref="MaxConnectionPoolSize"/>
    /// </summary>
    /// <remarks>
    /// The max idle connection pool size represents the maximum number of idle connections buffered by the driver. An
    /// idle connection is a connection that has already been connected to the database instance and doesn't need to
    /// re-initialize. Setting this value to <see cref="Infinite"/> results in the idle pool size to be assigned the same value
    /// as <see cref="MaxConnectionPoolSize"/>.
    /// </remarks>
    /// <remarks>Also see <see cref="MaxConnectionPoolSize"/></remarks>
    public int MaxIdleConnectionPoolSize
    {
        get => _maxIdleConnPoolSize == Infinite ? MaxConnectionPoolSize : _maxIdleConnPoolSize;
        internal set => _maxIdleConnPoolSize = value;
    }

    /// <summary>The max connection pool size.</summary>
    /// <remarks>
    /// The max connection pool size specifies the allowed maximum number of idle and current in-use connections by
    /// the driver. a.k.a. ConnectionPoolSize = IdleConnectionPoolSize + InUseConnectionSize. When a driver reaches its allowed
    /// maximum connection pool size, no new connections can be established. Instead all threads that require a new connection
    /// have to wait until a connection is available to reclaim. See <see cref="ConnectionAcquisitionTimeout"/>for the maximum
    /// waiting time to acquire an idle connection from the pool. Setting this value to <see cref="Infinite"/> will result in
    /// an infinite pool.
    /// </remarks>
    /// <remarks>Also see <see cref="MaxIdleConnectionPoolSize"/></remarks>
    public int MaxConnectionPoolSize { get; internal set; } = 100;

    /// <summary>
    /// The maximum waiting time to either acquire an idle connection from the pool when connection pool is full or
    /// create a new connection when pool is not full.
    /// </summary>
    public TimeSpan ConnectionAcquisitionTimeout { get; internal set; } = TimeSpan.FromMinutes(1);

    /// <summary>The connection timeout when establishing a connection with a server.</summary>
    public TimeSpan ConnectionTimeout { get; internal set; } = TimeSpan.FromSeconds(30);

    /// <summary>The socket keep alive option.</summary>
    public bool SocketKeepAlive { get; internal set; } = true;

    /// <summary>
    /// The idle timeout on pooled connections. A connection that has been idled in connection pool for longer than
    /// the given timeout is stale and will be closed once it is seen. Use <see cref="InfiniteInterval"/> to disable idle time
    /// checking.
    /// </summary>
    public TimeSpan ConnectionIdleTimeout { get; internal set; } = InfiniteInterval;

    /// <summary>
    /// The maximum connection lifetime on pooled connections. A connection that has been created for longer than the
    /// given time will be closed once it is seen. Use <see cref="InfiniteInterval"/> to disable connection lifetime checking.
    /// </summary>
    public TimeSpan MaxConnectionLifetime { get; internal set; } = TimeSpan.FromHours(1);

    /// <summary>The connections to support ipv6 addresses.</summary>
    public bool Ipv6Enabled { get; internal set; } = false;

    /// <summary>
    /// Gets or internal sets a custom server address resolver used by the routing driver to resolve the initial
    /// address used to create the driver. Such resolution happens: 1) during the very first rediscovery when driver is
    /// created. 2) when all the known routers from the current routing table have failed and driver needs to fallback to the
    /// initial address.
    /// </summary>
    public IServerAddressResolver Resolver { get; internal set; } = new PassThroughServerAddressResolver();

    /// <summary>Enable the driver level metrics. Internally used for testing and experimenting.</summary>
    internal bool MetricsEnabled { get; set; }

    /// <summary>The default read buffer size which the driver allocates for its internal buffers.</summary>
    public int DefaultReadBufferSize { get; internal set; } = Constants.DefaultReadBufferSize;

    /// <summary>
    /// The size when internal read buffers reach, will be released for garbage collection. If reading large records
    /// (nodes, relationships or paths) and experiencing too much garbage collection consider increasing this size to a
    /// reasonable amount depending on your data.
    /// </summary>
    public int MaxReadBufferSize { get; internal set; } = Constants.MaxReadBufferSize;

    /// <summary>The default write buffer size which the driver allocates for its internal buffers.</summary>
    public int DefaultWriteBufferSize { get; internal set; } = Constants.DefaultWriteBufferSize;

    /// <summary>
    /// The size when internal write buffers reach, will be released for garbage collection. If writing large values
    /// and experiencing too much garbage collection consider increasing this size to a reasonable amount depending on your
    /// data.
    /// </summary>
    public int MaxWriteBufferSize { get; internal set; } = Constants.MaxWriteBufferSize;

    /// <summary>
    /// The default fetch size. Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from server
    /// in batches. This fetch size defines how many records to pull in each batch. Use <see cref="Infinite"/> to disable
    /// batching and always pull all records in one batch instead.
    /// </summary>
    public long FetchSize
    {
        get => _fetchSize;
        internal set => _fetchSize = FetchSizeUtil.AssertValidFetchSize(value);
    }

    /// <summary>
    /// Used to get and set the User Agent string. If not used the default will be "neo4j-dotnet/x.y" where x is the
    /// major version and y is the minor version.
    /// </summary>
    public string UserAgent { get; set; } = ConnectionSettings.DefaultUserAgent;

    public INotificationsConfig NotificationsConfig { get; internal set; }
}
