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

namespace Neo4j.Driver.V1
{
    /// <summary>
    ///     The <see cref="IDriver"/> instance maintains the connections with a Neo4j database, providing an access point via the
    ///     <see cref="ISession" /> method.
    /// </summary>
    /// <remarks>
    ///     The Driver maintains a session pool buffering the <see cref="ISession" />s created by the user. 
    ///     The size of the buffer can be configured by the <see cref="Config.MaxIdleConnectionPoolSize" /> property on the <see cref="Config" /> when creating the Driver.
    /// </remarks>
    public interface IDriver : IDisposable
    {
        /// <summary>
        ///     Gets the <see cref="Uri" /> of the Neo4j database.
        /// </summary>
        [Obsolete("Use Result.Summary.Server.Address instead.")]
        Uri Uri { get; }

        /// <summary>
        /// Obtain a session with the default access mode <see cref="AccessMode.Write"/>.
        /// </summary>
        /// <returns>An <see cref="ISession"/> that could be used to execute statements.</returns>
        ISession Session();

        /// <summary>
        /// Obtain a session with the default <see cref="AccessMode"/>.
        /// </summary>
        /// <param name="defaultMode">The default access mode of the session. 
        /// If no access mode is specified when using the statement running methods inside this session,
        /// the statement will be executed in connections satisfying the default access mode.</param>
        /// <returns>An <see cref="ISession"/> that could be used to execute statements.</returns>
        ISession Session(AccessMode defaultMode);

        /// <summary>
        /// Obtain a session with the default <see cref="AccessMode.Write"/> and start bookmark.
        /// </summary>
        /// <param name="bookmark">A reference to a previous transaction. If the bookmark is provided,
        /// then the server hosting is at least as up-to-date as the transaction referenced by the supplied bookmark.
        /// Specify a bookmark if the statement excuted inside this session need to be chained after statements from other sessions.</param>
        /// <returns>An <see cref="ISession"/> that could be used to execute statements.</returns>
        ISession Session(string bookmark);

        /// <summary>
        /// Obtain a session with the default <see cref="AccessMode"/> and start bookmark.
        /// </summary>
        /// <param name="defaultMode">The default access mode of the session. 
        /// If no access mode is specified when using the statement running methods inside this session,
        /// the statement will be executed in connections satisfying the default access mode.</param>
        /// <param name="bookmark">A reference to a previous transaction. If the bookmark is provided,
        /// then the server hosting is at least as up-to-date as the transaction referenced by the supplied bookmark.
        /// Specify a bookmark if the statement excuted inside this session need to be chained after statements from other sessions.</param>
        /// <returns>An <see cref="ISession"/> that could be used to execute statements.</returns>
        ISession Session(AccessMode defaultMode, string bookmark);

        /// <summary>
        /// Obtain a session with the default <see cref="AccessMode"/> and a series of start bookmars.
        /// </summary>
        /// <param name="defaultMode">The default access mode of the session.
        /// If no access mode is specified when using the statement running methods inside this session,
        /// the statement will be executed in connections satisfying the default access mode.</param>
        /// <param name="bookmarks">References to previous transactions. If the bookmarks are provided,
        /// then the server hosting is at least as up-to-date as the transaction referenced by the supplied bookmarks.
        /// Specify bookmarks if the statement excuted inside this session need to be chained after statements from other sessions.</param>
        /// <returns>An <see cref="ISession"/> that could be used to execute statements.</returns>
        ISession Session(AccessMode defaultMode, IEnumerable<string> bookmarks);

        /// <summary>
        /// Obtain a session with the default <see cref="AccessMode.Write"/> access mode and a series of start bookmars.
        /// </summary>
        /// <param name="bookmarks">References to previous transactions. If the bookmarks are provided,
        /// then the server hosting is at least as up-to-date as the transaction referenced by the supplied bookmarks.
        /// Specify bookmarks if the statement excuted inside this session need to be chained after statements from other sessions.</param>
        /// <returns>An <see cref="ISession"/> that could be used to execute statements.</returns>
        ISession Session(IEnumerable<string> bookmarks);
    }
    /// <summary>
    /// Used by driver to route a cypher statement to a write server or a read server.
    /// </summary>
    public enum AccessMode
    {
        /// <summary>
        /// Requires cypher statememt to be carried out on a read server
        /// </summary>
        Read,
        /// <summary>
        /// Requires cypher statement to be executed on a write server
        /// </summary>
        Write
    }
}