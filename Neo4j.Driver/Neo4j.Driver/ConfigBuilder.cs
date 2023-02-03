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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver;

/// <summary>Provides a way to generate a <see cref="Config"/> instance fluently.</summary>
public sealed class ConfigBuilder
{
    private readonly Config _config;

    internal ConfigBuilder(Config config)
    {
        _config = config;
    }

    /// <summary>Builds the <see cref="Config"/> instance based on the previously internal set values.</summary>
    /// <remarks>> If no value was internal set for a property the defaults specified in <see cref="Config"/> will be used.</remarks>
    /// <returns>A <see cref="Config"/> instance.</returns>
    internal Config Build()
    {
        return _config;
    }

    /// <summary>Sets the <see cref="Config"/> to use TLS if <paramref name="level"/> is <c>Encrypted</c>.</summary>
    /// <param name="level">
    /// <see cref="EncryptionLevel.Encrypted"/> enables TLS for the connection,
    /// <see cref="EncryptionLevel.None"/> otherwise. See <see cref="EncryptionLevel"/> for more info
    /// </param>
    /// .
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithEncryptionLevel(EncryptionLevel level)
    {
        _config.NullableEncryptionLevel = level;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="TrustManager"/> to use while establishing trust via TLS. The <paramref name="manager"/>
    /// will not take effect if <see cref="Config.EncryptionLevel"/> decides to use no TLS encryption on the connections.
    /// </summary>
    /// <param name="manager">A <see cref="TrustManager"/> instance.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    /// <remarks>We recommend using WithCertificateTrustPaths or WithCertificates</remarks>
    public ConfigBuilder WithTrustManager(TrustManager manager)
    {
        _config.TrustManager = manager;
        return this;
    }

    /// <summary>Sets the <see cref="Config"/> to use a given <see cref="ILogger"/> instance.</summary>
    /// <param name="logger">The <see cref="ILogger"/> instance to use, if <c>null</c> no logging will occur.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithLogger(ILogger logger)
    {
        _config.Logger = logger;
        return this;
    }

    /// <summary>Sets the size of the idle connection pool.</summary>
    /// <param name="size">
    /// The size of the <see cref="Config.MaxIdleConnectionPoolSize"/>, internal set to 0 will disable
    /// connection pooling.
    /// </param>
    /// .
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithMaxIdleConnectionPoolSize(int size)
    {
        _config.MaxIdleConnectionPoolSize = size;
        return this;
    }

    /// <summary>Sets the size of the connection pool.</summary>
    /// <param name="size">The size of the <see cref="Config.MaxConnectionPoolSize"/></param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithMaxConnectionPoolSize(int size)
    {
        _config.MaxConnectionPoolSize = size;
        return this;
    }

    /// <summary>
    /// Sets the maximum connection acquisition timeout for waiting for a connection to become available in idle
    /// connection pool when <see cref="Config.MaxConnectionPoolSize"/> is reached.
    /// </summary>
    /// <param name="timeSpan">The connection acquisition timeout.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithConnectionAcquisitionTimeout(TimeSpan timeSpan)
    {
        _config.ConnectionAcquisitionTimeout = timeSpan;
        return this;
    }

    /// <summary>
    /// Specify socket connection timeout. A <see cref="TimeSpan"/> that represents the number of milliseconds to
    /// wait, or <see cref="Config.InfiniteInterval"/> to wait indefinitely.
    /// </summary>
    /// <param name="timeSpan">
    /// Represents the number of milliseconds to wait or <see cref="Config.InfiniteInterval"/> to wait
    /// indefinitely.
    /// </param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithConnectionTimeout(TimeSpan timeSpan)
    {
        _config.ConnectionTimeout = timeSpan;
        return this;
    }

    /// <summary>
    /// Enable socket to send keep alive pings on TCP level to prevent pooled socket connections from getting killed
    /// after leaving client idle for a long time. The interval of keep alive pings are internal set via your OS system.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithSocketKeepAliveEnabled(bool enable)
    {
        _config.SocketKeepAlive = enable;
        return this;
    }

    /// <summary>
    /// Specify the maximum time transactions are allowed to retry via transaction functions. These methods will retry
    /// the given unit of work on <see cref="SessionExpiredException"/>, <see cref="TransientException"/> and
    /// <see cref="ServiceUnavailableException"/> with exponential backoff using initial delay of 1 second. Default value is 30
    /// seconds.
    /// </summary>
    /// <param name="time">Specify the maximum retry time. </param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithMaxTransactionRetryTime(TimeSpan time)
    {
        _config.MaxTransactionRetryTime = time;
        return this;
    }

    /// <summary>
    /// Specify the connection idle timeout. The connection that has been idled in pool for longer than specified
    /// timeout will not be reused but closed.
    /// </summary>
    /// <param name="timeSpan">The max timespan that a connection can be reused after has been idle for.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithConnectionIdleTimeout(TimeSpan timeSpan)
    {
        _config.ConnectionIdleTimeout = timeSpan;
        return this;
    }

    /// <summary>
    /// Specify the maximum connection life time. The connection that has been created for longer than specified time
    /// will not be reused but closed.
    /// </summary>
    /// <param name="timeSpan">The max timespan that a connection can be reused after has been created for.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithMaxConnectionLifetime(TimeSpan timeSpan)
    {
        _config.MaxConnectionLifetime = timeSpan;
        return this;
    }

    /// <summary>Setting this option to true will enable ipv6 on socket connections.</summary>
    /// <param name="enable">true to enable ipv6, false to only support ipv4 addresses.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithIpv6Enabled(bool enable)
    {
        _config.Ipv6Enabled = enable;
        return this;
    }

    /// <summary>
    /// Gets or internal sets a custom server address resolver used by the routing driver to resolve the initial
    /// address used to create the driver. Such resolution happens: 1) during the very first rediscovery when driver is
    /// created. 2) when all the known routers from the current routing table have failed and driver needs to fallback to the
    /// initial address.
    /// </summary>
    /// <param name="resolver">The resolver, default to a resolver that simply pass the initial server address as it is.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithResolver(IServerAddressResolver resolver)
    {
        _config.Resolver = resolver;
        return this;
    }

    /// <summary>Specify the default read buffer size which the driver allocates for its internal buffers.</summary>
    /// <param name="defaultReadBufferSize">the buffer size</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithDefaultReadBufferSize(int defaultReadBufferSize)
    {
        _config.DefaultReadBufferSize = defaultReadBufferSize;
        return this;
    }

    /// <summary>Specify the size when internal read buffers reach, will be released for garbage collection.</summary>
    /// <param name="maxReadBufferSize">the buffer size</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    /// <remarks>
    /// If reading large records (nodes, relationships or paths) and experiencing too much garbage collection consider
    /// increasing this size to a reasonable amount depending on your data.
    /// </remarks>
    public ConfigBuilder WithMaxReadBufferSize(int maxReadBufferSize)
    {
        _config.MaxReadBufferSize = maxReadBufferSize;
        return this;
    }

    /// <summary>Specify the default write buffer size which the driver allocates for its internal buffers.</summary>
    /// <param name="defaultWriteBufferSize">the buffer size</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithDefaultWriteBufferSize(int defaultWriteBufferSize)
    {
        _config.DefaultWriteBufferSize = defaultWriteBufferSize;
        return this;
    }

    /// <summary>Specify the size when internal write buffers reach, will be released for garbage collection.</summary>
    /// <param name="maxWriteBufferSize">the buffer size</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    /// <remarks>
    /// If writing large values and experiencing too much garbage collection consider increasing this size to a
    /// reasonable amount depending on your data.
    /// </remarks>
    public ConfigBuilder WithMaxWriteBufferSize(int maxWriteBufferSize)
    {
        _config.MaxWriteBufferSize = maxWriteBufferSize;
        return this;
    }

    /// <summary>
    /// Sets the default fetch size. Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from
    /// server in batches. This fetch size defines how many records to pull in each batch. Use <see cref="Config.Infinite"/> to
    /// disable batching and always pull all records in one batch instead.
    /// </summary>
    /// <param name="size">The fetch size.</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithFetchSize(long size)
    {
        _config.FetchSize = size;
        return this;
    }

    internal ConfigBuilder WithMetricsEnabled(bool enabled)
    {
        _config.MetricsEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Sets the userAgent. Used to get and set the User Agent string. If not used the default will be
    /// "neo4j-dotnet/x.y" where x is the major version and y is the minor version.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    public ConfigBuilder WithUserAgent(string userAgent)
    {
        _config.UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        return this;
    }

    /// <summary>
    /// Sets the rule for which Certificate Authority(CA) certificates to use when building trust with a server
    /// certificate.
    /// </summary>
    /// <param name="certificateTrustRule">The rule for validating server certificates when using encryption.</param>
    /// <param name="trustedCaCertificates">
    /// Optional list of certificates to use to validate a server certificate. should only
    /// be set when <paramref name="certificateTrustRule"/> is <c>CertificateTrustRule.TrustList</c>
    /// </param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    /// <remarks>
    /// Used in conjunction with <see cref="WithEncryptionLevel"/>. Not to be used when using a non-basic Uri
    /// Scheme(+s, +ssc) on <see cref="GraphDatabase"/>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when mismatch between certificateTrustRule and trustedCaCertificates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when certificateTrustRule is not an expected enum.</exception>
    public ConfigBuilder WithCertificateTrustRule(
        CertificateTrustRule certificateTrustRule,
        IReadOnlyList<X509Certificate2> trustedCaCertificates = null)
    {
        _config.TrustManager = certificateTrustRule switch
        {
            CertificateTrustRule.TrustSystem when trustedCaCertificates != null =>
                throw new ArgumentException(
                    $"{nameof(trustedCaCertificates)} is not valid when {nameof(certificateTrustRule)} is {nameof(CertificateTrustRule.TrustSystem)}"),
            CertificateTrustRule.TrustAny when trustedCaCertificates != null =>
                throw new ArgumentException(
                    $"{nameof(trustedCaCertificates)} is not valid when {nameof(certificateTrustRule)} is {nameof(CertificateTrustRule.TrustAny)}"),
            CertificateTrustRule.TrustList when trustedCaCertificates == null || trustedCaCertificates.Count == 0 =>
                throw new ArgumentException(
                    $"{nameof(trustedCaCertificates)} must not be null or empty when {nameof(certificateTrustRule)} is {nameof(CertificateTrustRule.TrustList)}"),
            CertificateTrustRule.TrustSystem => TrustManager.CreateChainTrust(),
            CertificateTrustRule.TrustList => TrustManager.CreateCertTrust(trustedCaCertificates),
            CertificateTrustRule.TrustAny => TrustManager.CreateInsecure(),
            _ => throw new ArgumentOutOfRangeException(
                $"{certificateTrustRule} is not implemented in {nameof(ConfigBuilder)}.{nameof(WithCertificateTrustRule)}")
        };

        return this;
    }

    /// <summary>
    /// Sets the rule for which Certificate Authority(CA) certificates to use when building trust with a server
    /// certificate.
    /// </summary>
    /// <param name="certificateTrustRule">The rule for validating server certificates when using encryption.</param>
    /// <param name="trustedCaCertificateFileNames">
    /// Optional list of paths to certificates to use to validate a server
    /// certificate. should only be set when using <code>CertificateTrustRule.TrustList</code>
    /// </param>
    /// <returns>An <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
    /// <remarks>
    /// Used in conjunction with <see cref="WithEncryptionLevel"/>. Not to be used when using a non-basic Uri
    /// Scheme(+s, +ssc) on <see cref="GraphDatabase"/>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when mismatch between certificateTrustRule and
    /// trustedCaCertificateFileNames.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when certificateTrustRule is not an expected enum.</exception>
    public ConfigBuilder WithCertificateTrustRule(
        CertificateTrustRule certificateTrustRule,
        IReadOnlyList<string> trustedCaCertificateFileNames = null)
    {
        var certs = trustedCaCertificateFileNames?.Select(x => new X509Certificate2(x)).ToList();
        return WithCertificateTrustRule(certificateTrustRule, certs);
    }

    /// <summary>
    ///     Set which <see cref="INotification" />s the session can receive in <see cref="IResultSummary.Notifications" />
    ///     when executing a query, overriding any server configuration.
    ///     Overriding any driver configuration for queries executed in the session.
    /// </summary>
    /// <remarks>Cannot be used with: <see cref="WithNoNotifications" />.</remarks>
    /// <param name="minimumSeverity"></param>
    /// <param name="disabledCategories"></param>
    /// <returns>A <see cref="ConfigBuilder" /> instance for further configuration options.</returns>
    public ConfigBuilder WithNotifications(
        Severity minimumSeverity = Severity.Information,
        params Category[] disabledCategories)
    {
        _config.NotificationsConfig = new NotificationsConfig(minimumSeverity, disabledCategories);
        return this;
    }

    /// <summary>
    ///     Set session to not receive <see cref="INotification" />s from the server when executing
    ///     queries.
    ///     Overriding any driver configuration for queries executed in the session.
    /// </summary>
    /// <remarks>
    ///     Cannot be used with: <see cref="WithNotifications" />.
    /// </remarks>
    /// <returns>A <see cref="ConfigBuilder" /> instance for further configuration options.</returns>
    public ConfigBuilder WithNoNotifications()
    {
        _config.NotificationsConfig = new NoNotificationsConfig();
        return this;
    }
}
