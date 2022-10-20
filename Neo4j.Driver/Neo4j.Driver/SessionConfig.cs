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
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    /// <summary>
    /// The interface that defines options applicable to session constructions.
    /// It could be populated by the provided builder-style methods.
    /// The default <see cref="SessionConfig"/> defines a <see cref="AccessMode.Write"/> session
    /// with the server default database using default fetch size specified in <see cref="Config.FetchSize"/>.
    /// </summary>
    public sealed class SessionConfig
    {
        internal static readonly SessionConfig Default = new SessionConfig();
        private string _database;
        private IEnumerable<Bookmarks> _bookmarks;
        private long? _fetchSize;
        private string _impersonatedUser;

        internal SessionConfig()
        {
            DefaultAccessMode = AccessMode.Write;
            _database = null;
            _bookmarks = null;
            _fetchSize = null;
            _impersonatedUser = null;
        }

        internal static SessionConfigBuilder Builder => new SessionConfigBuilder(new SessionConfig());

        /// <summary>
        /// The database that the constructed session will connect to.
        ///
        /// <remarks>
        /// When used against servers supporting multi-databases, it is recommended that this value to be set explicitly
        /// either through <see cref="SessionConfigBuilder.WithDatabase"/> method.
        /// If not, then the session will connect to the default database configured on the server side.
        ///
        /// When used against servers that don't support multi-databases, this property should be left unset.
        /// </remarks>
        /// </summary>
        /// <exception cref="set_Database">throws <see cref="System.ArgumentNullException"/> when provided database name
        /// is null or an empty string.</exception>
        public string Database
        {
            get => _database;
            internal set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                _database = value;
            }
        }

        /// <summary>
        /// The type of access required by the constructed session.
        ///
        /// This is used to route the requests originating from this session instance to the correct server in a clustered
        /// environment.
        ///
        /// <remarks>The default access mode set is overriden when transaction functions (i.e.
        /// <see cref="IAsyncSession.ReadTransactionAsync{T}(System.Func{Neo4j.Driver.IAsyncTransaction,System.Threading.Tasks.Task{T}})"/> and
        /// <see cref="IAsyncSession.WriteTransactionAsync{T}(System.Func{Neo4j.Driver.IAsyncTransaction,System.Threading.Tasks.Task{T}})"/> is
        /// used (with corresponding access modes derived from invoked method name).
        /// </remarks>
        /// </summary>
        public AccessMode DefaultAccessMode { get; internal set; }

        /// <summary>
        /// The initial bookmarks to be used by the constructed session.
        ///
        /// The first transaction (either auto-commit or explicit) will ensure that the executing server is at least
        /// up to date to the point identified by the latest of the provided initial bookmarks. The bookmarks can be
        /// obtained from <see cref="IAsyncSession.LastBookmarks"/> (and corresponding properties in other types of
        /// sessions, i.e. IRxSession or ISession.
        /// </summary>
        public IEnumerable<Bookmarks> Bookmarks
        {
            get => _bookmarks;
            internal set => _bookmarks = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// The default fetch size.
        /// Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from server in batches.
        /// This fetch size defines how many records to pull in each batch.
        /// Use <see cref="Config.Infinite"/> to disable batching and always pull all records in one batch instead.
        /// </summary>
        public long? FetchSize
        {
            get => _fetchSize;
            internal set => _fetchSize = FetchSizeUtil.AssertValidFetchSize(value);
        }

        /// <summary>
        /// Allows the specification of a username that the user wants to impersonate for the duration of the
        /// session. Once set this cannot be changed for the duration of the sessions liftime. 
        /// </summary>
        /// <exception cref="set_ImpersonatedUser">throws <see cref="System.ArgumentNullException"/> when provided with a
        /// null or empty string</exception>
        public string ImpersonatedUser
        {
            get => _impersonatedUser;
            internal set => _impersonatedUser = (!string.IsNullOrEmpty(value)) ? value : throw new ArgumentNullException();
        }

        public IBookmarkManager BookmarkManager { get; set; }
        public NotificationFilter[] NotificationFilters { get; set; }
    }

    /// <summary>
    /// The builder to build a <see cref="SessionConfig"/>.
    /// </summary>
    public sealed class SessionConfigBuilder
    {
        private readonly SessionConfig _config;

        internal SessionConfigBuilder(SessionConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Returns an action on <see cref="SessionConfigBuilder"/> which will set the database name to the value specified.
        /// </summary>
        /// <param name="database">the database name</param>
        /// <returns>An action of <see cref="SessionConfigBuilder"/></returns>
        public static Action<SessionConfigBuilder> ForDatabase(string database)
        {
            return o => o.WithDatabase(database);
        }

        /// <summary>
        /// Sets the database the constructed session will connect to.
        /// </summary>
        /// <param name="database">the database name</param>
        /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
        /// <seealso cref="SessionConfig.Database"/>
        public SessionConfigBuilder WithDatabase(string database)
        {
            _config.Database = database;
            return this;
        }

        /// <summary>
        /// Sets the type of access required by the constructed session.
        /// </summary>
        /// <param name="defaultAccessMode">the access mode</param>
        /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
        /// <seealso cref="SessionConfig.DefaultAccessMode"/>
        public SessionConfigBuilder WithDefaultAccessMode(AccessMode defaultAccessMode)
        {
            _config.DefaultAccessMode = defaultAccessMode;
            return this;
        }

        /// <summary>
        /// Sets the initial bookmarks to be used by the constructed session.
        /// </summary>
        /// <param name="bookmarks">the initial bookmarks</param>
        /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
        /// <seealso cref="SessionConfig.Bookmarks"/>
        [Obsolete("Replaced by WithBookmarks. Will be removed in 6.0.")]
        public SessionConfigBuilder WithBookmarks(params Bookmark[] bookmark)
        {
            _config.Bookmarks = bookmark.Select(x => new InternalBookmarks(x.Values));
            return this;
        }        
        
        /// <summary>
        /// Sets the initial bookmarks to be used by the constructed session.
        /// </summary>
        /// <param name="bookmarks">the initial bookmarks</param>
        /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
        /// <seealso cref="SessionConfig.Bookmarks"/>
        public SessionConfigBuilder WithBookmarks(params Bookmarks[] bookmarks)
        {
            _config.Bookmarks = bookmarks;
            return this;
        }


        /// <summary>
        /// Sets the default fetch size.
        /// Since Bolt v4 (Neo4j 4.0+), the query running result (records) are pulled from server in batches.
        /// This fetch size defines how many records to pull in each batch.
        /// Use <see cref="Config.Infinite"/> to disable batching and always pull all records in one batch instead.
        /// </summary>
        /// <param name="size">Fetch size of each record batch.</param>
        /// <returns>this <see cref="SessionConfigBuilder"/> instance</returns>
        /// <seealso cref="SessionConfig.FetchSize"/>
        public SessionConfigBuilder WithFetchSize(long size)
        {
            _config.FetchSize = size;
            return this;
        }

        /// <summary>
        /// Allows the specification of a username that the user wants to impersonate for the duration of the
        /// session. Once set this cannot be changed for the duration of the sessions liftime. 
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
            
        /// <summary>
        /// marked as internal until API is solidified.
        /// </summary>
        internal SessionConfigBuilder WithBookmarkManager(IBookmarkManager bookmarkManager)
        {
            _config.BookmarkManager = bookmarkManager;
            return this;
        }



        /// <summary>
        /// Set which notifications the server will return when executing cypher in this session, overriding any server defaults and driver configuration(<see cref="ConfigBuilder.WithNotificationFilters"/>).<br/>
        /// <see cref="INotification"/>s can be accessed via <see cref="IResultSummary.Notifications"/>.
        /// </summary>
        /// <param name="filters">Filters to apply.</param>
        /// <returns>A <see cref="ConfigBuilder"/> instance for further configuration options.</returns>
        public SessionConfigBuilder WithNotificationFilters(params NotificationFilter[] filters)
        {
            _config.NotificationFilters = filters;
            return this;
        }
    }
}