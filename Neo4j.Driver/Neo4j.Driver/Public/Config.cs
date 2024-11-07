// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Buffers;
using System.IO.Pipelines;
using System.Reflection;
using System.Security.Authentication;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Util;

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
    static Config()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        DefaultUserAgent = $"neo4j-dotnet/{version.Major.ToString()}.{version.Minor.ToString()}";
    }

    internal static string DefaultUserAgent { get; }

    /// <summary>This const defines the value of infinite in terms of configuration properties.</summary>
    public const int Infinite = -1;

    /// <summary>This const defines the value of infinite interval in terms of configuration properties.</summary>
    public static readonly TimeSpan InfiniteInterval = TimeSpan.FromMilliseconds(-1);

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
    /// <para/>
    /// Note that if there is a client certificate provider set, the time taken to fetch the certificate will be
    /// included in the connection acquisition timeout, so if fetching the certificate is particularly slow, it might
    /// be necessary to increase the timeout.
    /// </summary>
    public TimeSpan ConnectionAcquisitionTimeout { get; internal set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Pooled connections that have been idle in the pool for longer than this timeout will be tested before they are
    /// used again, to ensure they are still live. If this option is set too low, an additional network call will
    /// be incurred when acquiring a connection, which causes a performance hit.
    /// <para/>
    /// If this is set high, you may receive sessions that are backed by no longer live connections, which will lead
    /// to exceptions in your application. Assuming the database is running, these exceptions will go away if you
    /// retry acquiring sessions.
    /// <para/>
    /// Hence, this parameter tunes a balance between the likelihood of your application seeing connection problems, and
    /// performance.
    /// <para/>
    /// You normally should not need to tune this parameter. No connection liveness check is done by default.
    /// Value 0 means connections will always be tested for validity. Values less than 0 are not allowed.
    /// </summary>
    public TimeSpan? ConnectionLivenessThreshold { get; internal set; }

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
    public long FetchSize { get; internal set; } = Constants.DefaultFetchSize;

    /// <summary>
    /// Used to get and set the User Agent string. If not used the default will be "neo4j-dotnet/x.y" where x is the
    /// major version and y is the minor version.
    /// </summary>
    public string UserAgent { get; internal set; } = DefaultUserAgent;

    /// <summary>
    /// The configuration for setting which notifications the server should send to the client.<br/> This
    /// configuration is used for all queries executed by the driver unless otherwise overriden by
    /// <see cref="SessionConfigBuilder"/> for the scope of a single session.
    /// </summary>
    /// <remarks>
    /// Note: Configuration support was introduced in server version 5.7.<br/> Servers currently will analyze all
    /// queries for all <see cref="NotificationCategory"/>s and <see cref="NotificationSeverity"/>s.
    /// </remarks>
    /// <seealso cref="ConfigBuilder.WithNotifications"/>
    /// <seealso cref="ConfigBuilder.WithNotificationsDisabled"/>
    /// <seealso cref="SessionConfig.NotificationsConfig"/>
    /// <seealso cref="INotification"/>
    /// <seealso cref="IResultSummary.Notifications"/>
    public INotificationsConfig NotificationsConfig { get; internal set; }

    /// <summary>
    /// The configuration for whether the driver attempts to send telemetry data.<br/>
    /// The telemetry collected covers high level usage of the driver and does not include any queries or
    /// parameters.<br/>
    /// Current collected metrics:
    /// <list type="bullet">
    ///     <item>Which method was used to start a transaction.</item>
    /// </list>
    /// Telemetry metrics are sent via Bolt to the uri provided when creating the driver instance or servers that make up
    /// the cluster members and Neo4j makes no attempt to collect these usage metrics from outside of AuraDB
    /// (Neo4j's cloud offering).<br/>
    /// Users can configure Neo4j server's collection collection behavior of client drivers telemetry data and log the
    /// telemetry data for diagnostics purposes.<br/>
    /// By default the driver allows the collection of this telemetry. 
    /// </summary>
    public bool TelemetryDisabled { get; set; }

    /// <summary>
    /// The configuration for the driver's underlying message reading from the network.
    /// </summary>
    public MessageReaderConfig MessageReaderConfig { get; internal set; }

    /// <summary>
    /// A certificate provider that will be used to provide the client certificate when
    /// a new connection is established.
    /// </summary>
    public IClientCertificateProvider ClientCertificateProvider { get; internal set; }

    /// <summary>
    /// The TLS version to use when establishing a connection.
    /// </summary>
    public SslProtocols TlsVersion { get; internal set; } = SslProtocols.Tls12;

    /// <summary>
    /// The negotiator to use when establishing a TLS connection. If this is null, the driver will use the default
    /// negotiator.|
    /// </summary>
    public ITlsNegotiator TlsNegotiator { get; internal set; }
}

/// <summary>
/// The configuration for the driver's underlying message reading from the network.
/// </summary>
public sealed class MessageReaderConfig
{
    /// <summary>
    /// Constructs a new instance of <see cref="MessageReaderConfig"/>.<br/>
    /// The configuration for the driver's underlying message reading from the network.<br/>
    /// Using this constructor overrides the <see cref="Config.DefaultReadBufferSize"/> and <see cref="Config.MaxReadBufferSize"/>.
    /// </summary>
    /// <param name="memoryPool">The memory pool for creating buffers when reading messages. The PipeReader will borrow
    /// memory from the pool of at least ReadBufferSize size. The message reader can request larger memory blocks to
    /// host an entire message. User code can provide an implementation for monitoring; by default, the driver will
    /// allocate a new array pool that does not take advantage of shared memory pools.</param>
    /// <param name="minBufferSize">The minimum buffer size to use when renting memory from the pool.
    /// <br/>The default value is 32,768.</param>
    /// <param name="maxPooledBufferSize">The maximum buffer size to use when renting memory from neo4j's default pool.
    /// <br/>The default is 131,072.</param>
    /// <seealso cref="PipeReader"/>
    /// <seealso cref="MemoryPool{T}"/>
    /// <seealso cref="StreamPipeReaderOptions"/>
    /// <remarks>
    /// To optimize the memory usage of the driver pass .NET's shared memory pool(<see cref="MemoryPool{T}.Shared"/>) as
    /// the <paramref name="memoryPool"/>, this should only be used when there is complete trust over the usage of
    /// shared memory buffers in the application as other components may be using the same memory pool.
    /// </remarks>
    /// <remarks>
    /// The <paramref name="memoryPool"/> will define it's own maximum pooled buffer size, but must be able to provide
    /// an memory object upto the limit 2146435071 bytes. The <paramref name="maxPooledBufferSize"/> will not be observed
    /// when the <paramref name="memoryPool"/> is passed..
    /// </remarks>
    /// <remarks>
    /// Note using a small value for <paramref name="minBufferSize"/> could cause a degradation in performance.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="minBufferSize"/>is less than 1 or greater than 2146435071
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="maxPooledBufferSize"/> is less than <paramref name="minBufferSize"/> or greater than 2146435071
    /// </exception>
    public MessageReaderConfig(MemoryPool<byte> memoryPool = null, int minBufferSize = -1, int maxPooledBufferSize = -1)
    {
        const int maxArrayLength = 2146435071;
        if (minBufferSize is < -1 or 0 or > maxArrayLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minBufferSize), minBufferSize,
                "Minimum buffer size must be between 1 and 2146435071, leave as -1 to use default.");
        }
        MinBufferSize = minBufferSize == -1 ? Constants.DefaultReadBufferSize : MinBufferSize;
        if (maxPooledBufferSize != -1 && (maxPooledBufferSize < MinBufferSize || maxPooledBufferSize > maxArrayLength))
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPooledBufferSize),
                maxPooledBufferSize,
                $"Max pooled buffer size buffer size must be greater than minBufferSize({MinBufferSize}), leave as -1 to use default.");
        }
        
        DisablePipelinedMessageReader = false;
        MemoryPool = memoryPool ?? new PipeReaderMemoryPool(MinBufferSize,
            maxPooledBufferSize == -1 ? Constants.MaxReadBufferSize : maxPooledBufferSize);
        StreamPipeReaderOptions = new(MemoryPool, MinBufferSize, leaveOpen: true);
    }

    internal MessageReaderConfig(Config config)
    {
        DisablePipelinedMessageReader = false;
        MinBufferSize = config.DefaultReadBufferSize;
        MemoryPool = new PipeReaderMemoryPool(config.DefaultReadBufferSize, config.MaxReadBufferSize);
        StreamPipeReaderOptions = new(MemoryPool, config.DefaultReadBufferSize, leaveOpen: true);
    }

    /// <summary>
    /// As of 5.15, the driver has migrated the underlying message reading mechanism utilizing <see cref="PipeReader"/>;
    /// this optimizes the reading and memory usage of the driver, and setting this to true will revert the driver to
    /// the legacy message reader.
    /// </summary>
    internal bool DisablePipelinedMessageReader { get; }
    
    /// <summary>
    /// The memory pool for creating buffers when reading messages. The PipeReader will borrow memory from the pool of
    /// at least <see cref="MinBufferSize"/> size. The message reader can request larger memory blocks to host
    /// an entire message. User code can provide an implementation for monitoring; by default, the driver will allocate
    /// a new array pool that does not take advantage of shared memory pools.
    /// </summary>
    public MemoryPool<byte> MemoryPool { get; }
    
    /// <summary>
    /// The minimum buffer size to use when renting memory from the pool. The default value is 65,539.
    /// </summary>
    public int MinBufferSize { get; }

    internal StreamPipeReaderOptions StreamPipeReaderOptions { get; }
}
