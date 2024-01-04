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
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// The <see cref="IDriver"/> instance maintains the connections with a Neo4j database, providing an access point
/// via the <see cref="IAsyncSession"/> method.
/// </summary>
/// <remarks>
/// The Driver maintains a connection pool buffering connections created by the user. The size of the buffer can
/// be configured by the <see cref="Neo4j.Driver.Config.MaxConnectionPoolSize"/> property on the
/// <see cref="Neo4j.Driver.Config"/> when creating the Driver.
/// </remarks>
public interface IDriver : IDisposable, IAsyncDisposable
{
    /// <summary>Gets the driver configurations.</summary>
    Config Config { get; }

    /// <summary>Gets Encrypted status</summary>
    bool Encrypted { get; }

    /// <summary>Obtain a session with the default <see cref="SessionConfig"/>.</summary>
    /// <returns>An <see cref="IAsyncSession"/> that could be used to execute queries.</returns>
    IAsyncSession AsyncSession();

    /// <summary>Obtain a session with the customized <see cref="SessionConfig"/>.</summary>
    /// <param name="action">
    /// An action, provided with a <see cref="SessionConfigBuilder"/> instance, that should populate the
    /// provided instance with desired <see cref="SessionConfig"/>.
    /// </param>
    /// <returns>An <see cref="IAsyncSession"/> that could be used to execute queries.</returns>
    IAsyncSession AsyncSession(Action<SessionConfigBuilder> action);

    /// <summary>
    /// Asynchronously releases all resources (connection pools, connections, etc) associated with this IDriver
    /// instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    [Obsolete("Replaced by DisposeAsync")]
    Task CloseAsync();

    /// <summary>
    /// Asynchronously verify if the driver can connect to the remote server returning server info. If the driver
    /// fails to connect to the remote server, an error will be thrown, which can be used to further understand the cause of
    /// the connectivity issue. Note: Even if this method failed with an error, the driver still need to be closed via
    /// <see cref="CloseAsync"/> to free up all resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the connected server's info.</returns>
    Task<IServerInfo> GetServerInfoAsync();

    /// <summary>Asynchronously verify if the driver can connect to the remote server.</summary>
    /// <remarks>
    /// Even if this method returns false, the driver still need to be closed via <see cref="CloseAsync"/> or disposed
    /// to free up all resources.
    /// </remarks>
    /// <returns>
    /// A task that represents the asynchronous operation.<br/> The task result contains if the driver successfully
    /// connected to the remote server.
    /// </returns>
    Task<bool> TryVerifyConnectivityAsync();

    /// <summary>
    /// Asynchronously verify if the driver can connect to the remote server by establishing a network connection with
    /// the remote. If the driver fails to connect to the remote server, an error will be thrown, which can be used to further
    /// understand the cause of the connectivity issue. Note: Even if this method failed with an error, the driver still need
    /// to be closed via <see cref="CloseAsync"/> to free up all resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous verification operation.</returns>
    Task VerifyConnectivityAsync();

    /// <summary>
    /// Asynchronously verify if the driver connects to a server and/or cluster that can support multi-database
    /// feature.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains True if the remote server and/or
    /// cluster support multi-databases, otherwise false.
    /// </returns>
    Task<bool> SupportsMultiDbAsync();

    /// <summary>Asynchronously verify if the driver supports re-auth.</summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains True if the remote server and/or
    /// cluster support re-auth, otherwise false.
    /// </returns>
    Task<bool> SupportsSessionAuthAsync();

    /// <summary>
    /// Gets an <see cref="IExecutableQuery&lt;IRecord, IRecord&gt;"/> that can be used to configure and execute a
    /// query using fluent method chaining.
    /// </summary>
    /// <param name="cypher">The cypher of the query.</param>
    /// <returns>
    /// An <see cref="IExecutableQuery&lt;IRecord, IRecord&gt;"/> that can be used to configure and execute a query
    /// using fluent method chaining.
    /// </returns>
    IExecutableQuery<IRecord, IRecord> ExecutableQuery(string cypher);

    /// <summary>
    /// Asynchronously verify if the driver can connect using the <paramref name="authToken"/>.<br/> Try to establish
    /// a working read connection to the remote server or a member of a cluster and exchange some data.In a cluster, there is
    /// no guarantee about which server will be contacted. If the data exchange is successful and the authentication
    /// information is valid, true is returned. Otherwise, the error will be matched against a list of known authentication
    /// errors.If the error is on that list, false is returned indicating that the authentication information is invalid. If
    /// the not on the list, the error is thrown.
    /// </summary>
    /// <param name="authToken">Auth token to verify.</param>
    /// <returns> A task that represents the asynchronous operation. </returns>
    Task<bool> VerifyAuthenticationAsync(IAuthToken authToken);
}
