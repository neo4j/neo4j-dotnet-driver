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
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver;

/// <summary>
/// The interface that defines options applicable to session constructions. It could be populated by the provided
/// builder-style methods. The default <see cref="SessionConfig"/> defines a <see cref="AccessMode.Write"/> session with
/// the server default database using default fetch size specified in <see cref="Config.FetchSize"/>.
/// </summary>
public sealed class SessionConfig
{
    internal static readonly SessionConfig Default = new();
    private IEnumerable<Bookmarks> _bookmarks;
    private string _impersonatedUser;

    internal SessionConfig()
    {
        DefaultAccessMode = AccessMode.Write;
        Database = null;
        _bookmarks = null;
        FetchSize = null;
        _impersonatedUser = null;
    }

    internal SessionConfig(string impersonatedUser) : this()
    {
        _impersonatedUser = impersonatedUser;
    }

    internal SessionConfig(IAuthToken authToken) : this()
    {
        AuthToken = authToken;
    }

    internal static SessionConfigBuilder Builder => new(new SessionConfig());

    /// <summary>Gets the target database name for queries executed within the constructed session.</summary>
    /// <remarks>
    /// This option has no explicit value by default, as such it is recommended to set a value if the target database is known
    /// in advance. This has the benefit of ensuring a consistent target database name throughout the session in a
    /// straightforward way and potentially simplifies driver logic, which reduces network communication and might result in
    /// better performance.<br/><br/> Cypher clauses such as USE are not a replacement for this option as Cypher is handled by
    /// the server and not the driver.<br/><br/> When no explicit name is set, the driver behavior depends on the connection
    /// URI scheme supplied to the driver on instantiation and Bolt protocol version.<br/><br/> Specifically, the following
    /// applies:
    /// <list type="bullet">
    ///     <item>
    ///     <b>bolt schemes</b> - queries are dispatched to the server for execution without explicit database name
    ///     supplied, meaning that the target database name for query execution is determined by the server. It is important to
    ///     note that the target database may change (even within the same session), for instance if the user's home database
    ///     is changed on the server.
    ///     </item>
    ///     <item>
    ///     <b>neo4j schemes</b>  - providing that Bolt protocol version 4.4, which was introduced with Neo4j server 4.4,
    ///     or above is available, the driver fetches the user's home database name from the server on first query execution
    ///     within the session and uses the fetched database name explicitly for all queries executed within the session. This
    ///     ensures that the database name remains consistent within the given session. For instance, if the user's home
    ///     database name is 'movies' and the server supplies it to the driver upon database name fetching for the session, all
    ///     queries within that session are executed with the explicit database name 'movies' supplied. Any change to the
    ///     user’s home database is reflected only in sessions created after such change takes effect. This behavior requires
    ///     additional network communication. In clustered environments, it is strongly recommended to avoid a single point of
    ///     failure. For instance, by ensuring that the connection URI resolves to multiple endpoints. For older Bolt protocol
    ///     versions the behavior is the same as described for the bolt schemes above.
    ///     </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SessionConfigBuilder.WithDatabase"/>
    public string Database { get; internal set; }

    /// <summary>
    /// The type of access required by the constructed session. This is used to route the requests originating from this
    /// session instance to the correct server in a clustered environment.
    /// <remarks>
    /// The default access mode set is overriden when transaction functions (i.e.
    /// <see
    ///     cref="IAsyncSession.ExecuteReadAsync{T}(System.Func{Neo4j.Driver.IAsyncQueryRunner,System.Threading.Tasks.Task{T}}, System.Action{TransactionConfigBuilder})"/>
    /// and
    /// <see
    ///     cref="IAsyncSession.ExecuteWriteAsync{T}(System.Func{Neo4j.Driver.IAsyncQueryRunner,System.Threading.Tasks.Task{T}}, System.Action{TransactionConfigBuilder})"/>
    /// is used (with corresponding access modes derived from invoked method name).
    /// </remarks>
    /// </summary>
    public AccessMode DefaultAccessMode { get; internal set; }

    /// <summary>
    /// The initial bookmarks to be used by the constructed session. The first transaction (either auto-commit or
    /// explicit) will ensure that the executing server is at least up to date to the point identified by the latest of the
    /// provided initial bookmarks. The bookmarks can be obtained from <see cref="IAsyncSession.LastBookmarks"/> (and
    /// corresponding properties in other types of sessions, i.e. IRxSession or ISession.
    /// </summary>
    public IEnumerable<Bookmarks> Bookmarks
    {
        get => _bookmarks;
        internal set => _bookmarks = value ?? throw new ArgumentNullException();
    }

    /// <summary>
    /// The default fetch size. Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from server
    /// in batches. This fetch size defines how many records to pull in each batch. Use <see cref="Config.Infinite"/> to
    /// disable batching and always pull all records in one batch instead.
    /// </summary>
    public long? FetchSize { get; internal set; }

    /// <summary>
    /// Allows the specification of a username that the user wants to impersonate for the duration of the session.
    /// Once set this cannot be changed for the duration of the session's lifetime.
    /// </summary>
    /// <exception cref="set_ImpersonatedUser">
    /// throws <see cref="System.ArgumentNullException"/> when provided with a null or
    /// empty string
    /// </exception>
    public string ImpersonatedUser
    {
        get => _impersonatedUser;
        internal set => _impersonatedUser = !string.IsNullOrEmpty(value) ? value : throw new ArgumentNullException();
    }

    /// <summary>The auth token that will be used for this session. This overrides any auth settings on the driver object.</summary>
    public IAuthToken AuthToken { get; internal set; }

    internal IBookmarkManager BookmarkManager { get; set; }

    /// <summary>
    /// The configuration for setting which notifications the server should send to the client for queries executed in
    /// the session.
    /// </summary>
    /// <remarks>
    /// Note: Configuration support was introduced in server version 5.7.<br/> Servers currently will analyze all
    /// queries for all <see cref="NotificationCategory"/>s and <see cref="NotificationSeverity"/>s.
    /// </remarks>
    /// <seealso cref="SessionConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/>
    /// <seealso cref="SessionConfigBuilder.WithNotificationsDisabled"/>
    /// <seealso cref="Config.NotificationsConfig"/>
    /// <seealso cref="INotification"/>
    /// <seealso cref="IResultSummary.Notifications"/>
    public INotificationsConfig NotificationsConfig { get; internal set; }
}

/// <summary>The builder to build a <see cref="SessionConfig"/>.</summary>
public sealed class SessionConfigBuilder
{
    private readonly SessionConfig _config;

    internal SessionConfigBuilder(SessionConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Returns an action on <see cref="SessionConfigBuilder"/> which will set the database name to the value
    /// specified.
    /// </summary>
    /// <param name="database">the database name</param>
    /// <returns>An action of <see cref="SessionConfigBuilder"/></returns>
    public static Action<SessionConfigBuilder> ForDatabase(string database)
    {
        return o => o.WithDatabase(database);
    }

    /// <summary>Sets the target database name for queries executed within the constructed session.</summary>
    /// <param name="database">The database name.</param>
    /// <returns>This <see cref="SessionConfigBuilder"/> instance.</returns>
    /// <seealso cref="SessionConfig.Database"/>
    /// <remarks>
    /// This option has no explicit value by default, as such it is recommended to set a value if the target database is known
    /// in advance. This has the benefit of ensuring a consistent target database name throughout the session in a
    /// straightforward way and potentially simplifies driver logic, which reduces network communication and might result in
    /// better performance.<br/><br/> Cypher clauses such as USE are not a replacement for this option as Cypher is handled by
    /// the server and not the driver.<br/><br/> When no explicit name is set, the driver behavior depends on the connection
    /// URI scheme supplied to the driver on instantiation and Bolt protocol version.<br/><br/> Specifically, the following
    /// applies:
    /// <list type="bullet">
    ///     <item>
    ///     <b>bolt schemes</b> - queries are dispatched to the server for execution without explicit database name
    ///     supplied, meaning that the target database name for query execution is determined by the server. It is important to
    ///     note that the target database may change (even within the same session), for instance if the user's home database
    ///     is changed on the server.
    ///     </item>
    ///     <item>
    ///     <b>neo4j schemes</b>  - providing that Bolt protocol version 4.4, which was introduced with Neo4j server 4.4,
    ///     or above is available, the driver fetches the user's home database name from the server on first query execution
    ///     within the session and uses the fetched database name explicitly for all queries executed within the session. This
    ///     ensures that the database name remains consistent within the given session. For instance, if the user's home
    ///     database name is 'movies' and the server supplies it to the driver upon database name fetching for the session, all
    ///     queries within that session are executed with the explicit database name 'movies' supplied. Any change to the
    ///     user’s home database is reflected only in sessions created after such change takes effect. This behavior requires
    ///     additional network communication. In clustered environments, it is strongly recommended to avoid a single point of
    ///     failure. For instance, by ensuring that the connection URI resolves to multiple endpoints. For older Bolt protocol
    ///     versions the behavior is the same as described for the bolt schemes above.
    ///     </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Throws <see cref="System.ArgumentNullException"/> when provided database name
    /// is null or an empty string.
    /// </exception>
    public SessionConfigBuilder WithDatabase(string database)
    {
        if (string.IsNullOrEmpty(database))
        {
            throw new ArgumentNullException(nameof(database));
        }

        _config.Database = database;
        return this;
    }

    /// <summary>Sets the type of access required by the constructed session.</summary>
    /// <param name="defaultAccessMode">the access mode</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
    /// <seealso cref="SessionConfig.DefaultAccessMode"/>
    public SessionConfigBuilder WithDefaultAccessMode(AccessMode defaultAccessMode)
    {
        _config.DefaultAccessMode = defaultAccessMode;
        return this;
    }

    /// <summary>
    /// Use this overwrite the authentication information for the session. This requires the server to support
    /// re-authentication on the protocol level. You can check this by calling <see cref="IDriver.SupportsSessionAuthAsync"/>.
    /// <para/>
    /// It is not possible to overwrite the authentication information for the session with no authentication, i.e., downgrade
    /// the authentication at session level. Instead, you should create a driver with no authentication and upgrade the
    /// authentication at session level as needed.
    /// </summary>
    /// <param name="authToken">The auth token.</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
    /// <seealso cref="SessionConfig.AuthToken"/>
    public SessionConfigBuilder WithAuthToken(IAuthToken authToken)
    {
        _config.AuthToken = authToken;
        return this;
    }

    /// <summary>Sets the initial bookmarks to be used by the constructed session.</summary>
    /// <param name="bookmark">the initial bookmarks</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
    /// <seealso cref="SessionConfig.Bookmarks"/>
    [Obsolete("Replaced by WithBookmarks. Will be removed in 6.0.")]
    public SessionConfigBuilder WithBookmarks(params Bookmark[] bookmark)
    {
        _config.Bookmarks = bookmark.Select(x => new InternalBookmarks(x.Values));
        return this;
    }

    /// <summary>Sets the initial bookmarks to be used by the constructed session.</summary>
    /// <param name="bookmarks">the initial bookmarks</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
    /// <seealso cref="SessionConfig.Bookmarks"/>
    public SessionConfigBuilder WithBookmarks(params Bookmarks[] bookmarks)
    {
        _config.Bookmarks = bookmarks;
        return this;
    }

    /// <summary>
    /// Sets the default fetch size. Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from
    /// server in batches. This fetch size defines how many records to pull in each batch. Use <see cref="Config.Infinite"/> to
    /// disable batching and always pull all records in one batch instead.
    /// </summary>
    /// <param name="size">Fetch size of each record batch.</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
    /// <seealso cref="SessionConfig.FetchSize"/>
    public SessionConfigBuilder WithFetchSize(long size)
    {
        if (size <= 0 && size != Config.Infinite)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                $"The {nameof(SessionConfig)} fetch size may not be 0 or negative. Illegal record fetch size: {size}.");
        }

        _config.FetchSize = size;
        return this;
    }

    /// <summary>
    /// Allows the specification of a username that the user wants to impersonate for the duration of the session.
    /// Once set this cannot be changed for the duration of the session's lifetime.
    /// </summary>
    /// <param name="impersonatedUser">username that the user wants to impersonate</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
    public SessionConfigBuilder WithImpersonatedUser(string impersonatedUser)
    {
        _config.ImpersonatedUser = impersonatedUser;
        return this;
    }

    internal SessionConfig Build()
    {
        return _config;
    }

    /// <summary>Sets the <see cref="IBookmarkManager"/> for maintaining bookmarks for the lifetime of the session.</summary>
    /// <param name="bookmarkManager">An instance of <see cref="IBookmarkManager"/> to use in the session.</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance.</returns>
    public SessionConfigBuilder WithBookmarkManager(IBookmarkManager bookmarkManager)
    {
        _config.BookmarkManager = bookmarkManager;
        return this;
    }

    /// <summary>
    /// Override configuration for which <see cref="INotification"/>s should be emitted for the lifetime of the
    /// session. <br/> Unspecified configuration will be provided by configuration specified in the server or the driver's
    /// <see cref="ConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/>. <br/> If the driver has disabled notifications
    /// with <see cref="ConfigBuilder.WithNotificationsDisabled"/>, the unspecified values will be provided by the server.
    /// <br/> Disabling categories or severities allows the server to skip analysis for those, which can speed up query
    /// execution.
    /// </summary>
    /// <remarks>Cannot be used with: <see cref="WithNotificationsDisabled"/>.</remarks>
    /// <param name="minimumSeverity">
    /// Optional parameter to override the minimum severity of notifications emitted. <br/> By
    /// leaving null, the value will inherit configuration from
    /// <see cref="ConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/> or the server.
    /// </param>
    /// <param name="disabledCategories">
    /// Optional parameter to override the category of notifications emitted. <br/> By passing
    /// an empty collection, all categories are enabled.<br/> By leaving null, the value will inherit configuration from
    /// <see cref="ConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/> or the server.
    /// </param>
    /// <param name="disabledClassifications">
    /// Optional parameter to override the classification of notifications emitted. <br/> By passing
    /// an empty collection, all classifications are enabled.<br/> By leaving null, the value will inherit configuration from the
    /// server.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when all parameters are null.</exception>
    /// <returns>A <see cref="SessionConfigBuilder"/> instance for further configuration options.</returns>
    /// <seealso cref="WithNotificationsDisabled"/>
    /// <seealso cref="ConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/>
    /// <seealso cref="ConfigBuilder.WithNotificationsDisabled"/>
    public SessionConfigBuilder WithNotifications(
        Severity? minimumSeverity,
        Category[] disabledCategories = null,
        Classification[] disabledClassifications = null)
    {
        if (minimumSeverity == null && disabledCategories == null && disabledClassifications == null)
        {
            throw new ArgumentException(
                $"At least one of {nameof(minimumSeverity)}, {nameof(disabledCategories)}, " +
                $"or {nameof(disabledClassifications)} must be set.");
        }

        Category[] categoriesToDisable = null;
        if (disabledCategories != null || disabledClassifications != null)
        {
            var classificationsAsCategories = disabledClassifications?.Select(x => (Category)(int)x) ?? [];
            categoriesToDisable = (disabledCategories ?? []).Concat(classificationsAsCategories).ToArray();
        }

        _config.NotificationsConfig = new NotificationsConfig(minimumSeverity, categoriesToDisable);
        return this;
    }

    /// <summary>Disable all notifications for the lifetime of the session.</summary>
    /// <remarks>Cannot be used with: <see cref="WithNotifications(Severity?, Category[], Classification[])"/>.</remarks>
    /// <returns>A <see cref="SessionConfigBuilder"/> instance for further configuration options.</returns>
    /// <seealso cref="WithNotifications(Severity?, Category[], Classification[])"/>
    /// <seealso cref="ConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/>
    /// <seealso cref="ConfigBuilder.WithNotificationsDisabled"/>
    public SessionConfigBuilder WithNotificationsDisabled()
    {
        _config.NotificationsConfig = new NotificationsDisabledConfig();
        return this;
    }
}
