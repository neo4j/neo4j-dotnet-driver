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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    /// <summary>
    ///     The <see cref="IDriver"/> instance maintains the connections with a Neo4j database, providing an access point via the
    ///     <see cref="IAsyncSession" /> method.
    /// </summary>
    /// <remarks>
    ///     The Driver maintains a connection pool buffering connections created by the user.
    ///     The size of the buffer can be configured by the <see cref="Config.MaxConnectionPoolSize" /> property on the <see cref="Config" /> when creating the Driver.
    /// </remarks>
    public interface IDriver : IDisposable
    {
        /// <summary>
        /// Obtain a session with the default <see cref="SessionOptions"/>.
        /// </summary>
        /// <returns>An <see cref="IAsyncSession"/> that could be used to execute statements.</returns>
        IAsyncSession AsyncSession();

        /// <summary>
        /// Obtain a session with the customized <see cref="SessionOptions"/>.
        /// </summary>
        /// <param name="optionsBuilder">An action, provided with a <see cref="SessionOptions"/> instance, that should populate
        /// the provided instance with desired options.</param> 
        /// <returns>An <see cref="IAsyncSession"/> that could be used to execute statements.</returns>
        IAsyncSession AsyncSession(Action<SessionOptions> optionsBuilder);

        /// <summary>
        /// Asynchronously releases all resources (connection pools, connections, etc) associated with this IDriver instance.
        /// </summary>
        /// <returns>The close task.</returns>
        Task CloseAsync();

        /// <summary>
        /// Asynchronously verify if the driver can connect to the remote server by establishing a network connection with the remote.
        /// If the driver fails to connect to the remote server, an error will be thrown,
        /// which can be used to further understand the cause of the connectivity issue.
        /// Note: Even if this method failed with an error, the driver still need to be closed via <see cref="CloseAsync"/> to free up all resources.
        /// </summary>
        /// <returns>The verification task.</returns>
        Task VerifyConnectivityAsync();
    }
}