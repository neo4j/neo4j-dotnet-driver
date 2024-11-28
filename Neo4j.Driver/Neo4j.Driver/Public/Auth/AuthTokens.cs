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
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver;

/// <summary>
/// This provides methods to create <see cref="IAuthToken"/>s for various authentication schemes supported by this
/// driver. The scheme used must be also supported by the Neo4j instance you are connecting to.
/// </summary>
/// <remarks>
///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
/// </remarks>
public static class AuthTokens
{
    /// <summary>
    /// Gets an authentication token that can be used to connect to Neo4j instances with auth disabled. This will only
    /// work if authentication is disabled on the Neo4j Instance we are connecting to.
    /// </summary>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    public static IAuthToken None => new AuthToken(new Dictionary<string, object> { { AuthToken.SchemeKey, "none" } });

    /// <summary>The basic authentication scheme, using a username and a password.</summary>
    /// <param name="username">This is the "principal", identifying who this token represents.</param>
    /// <param name="password">This is the "credential", proving the identity of the user.</param>
    /// <returns>An authentication token that can be used to connect to Neo4j.</returns>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    public static IAuthToken Basic(string username, string password)
    {
        return new BasicAuthToken(username, password);
    }

    /// <summary>The basic authentication scheme, using a username and a password.</summary>
    /// <param name="username">This is the "principal", identifying who this token represents.</param>
    /// <param name="password">This is the "credential", proving the identity of the user.</param>
    /// <param name="realm">
    /// This is the "realm", specifies the authentication provider. If none is given, default to be decided
    /// by the server.
    /// </param>
    /// <returns>An authentication token that can be used to connect to Neo4j.</returns>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    public static IAuthToken Basic(string username, string password, string realm)
    {
        return new BasicAuthToken(username, password, realm);
    }

    /// <summary>The kerberos authentication scheme, using a base64 encoded ticket.</summary>
    /// <param name="base64EncodedTicket">A base64 encoded service ticket.</param>
    /// <returns>an authentication token that can be used to connect to Neo4j.</returns>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    public static IAuthToken Kerberos(string base64EncodedTicket)
    {
        return new KerberosAuthToken(base64EncodedTicket);
    }

    /// <summary>
    /// Gets an authentication token that can be used to connect to Neo4j instances with auth disabled. This will only
    /// work if authentication is disabled on the Neo4j Instance we are connecting to.
    /// </summary>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    /// <param name="principal">This is used to identify who this token represents.</param>
    /// <param name="credentials">This is credentials authenticating the principal.</param>
    /// <param name="realm">This is the "realm", specifies the authentication provider.</param>
    /// <param name="scheme">This is the authentication scheme, specifying what kind of authentication that should be used.</param>
    /// <returns>An authentication token that can be used to connect to Neo4j.</returns>
    public static IAuthToken Custom(string principal, string credentials, string realm, string scheme)
    {
        return new CustomAuthToken(principal, credentials, realm, scheme);
    }

    /// <summary>
    /// Gets an authentication token that can be used to connect to Neo4j instances with auth disabled. This will only
    /// work if authentication is disabled on the Neo4j Instance we are connecting to.
    /// </summary>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    /// <param name="principal">This is used to identify who this token represents.</param>
    /// <param name="credentials">This is credentials authenticating the principal.</param>
    /// <param name="realm">This is the "realm", specifies the authentication provider.</param>
    /// <param name="scheme">This is the authentication scheme, specifying what kind of authentication that should be used.</param>
    /// <param name="parameters">
    /// Extra parameters to be sent along the authentication provider. If none is given, then no extra
    /// parameters will be added.
    /// </param>
    /// <returns>An authentication token that can be used to connect to Neo4j.</returns>
    public static IAuthToken Custom(string principal, string credentials, string realm, string scheme, Dictionary<string, object> parameters)
    {
        return new CustomAuthToken(principal, credentials, realm, scheme, parameters);
    }

    /// <summary>
    /// The bearer authentication scheme, using a base64 encoded token, such as those supplied by SSO providers. Gets
    /// an authentication token that can be used to connect to Neo4j.
    /// </summary>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder})"/>
    /// </remarks>
    /// <param name="token">Base64 encoded token</param>
    /// <returns>An authentication token that can be used to connect to Neo4j.</returns>
    public static IAuthToken Bearer(string token)
    {
        return new BearerAuthToken(token);
    }
}
