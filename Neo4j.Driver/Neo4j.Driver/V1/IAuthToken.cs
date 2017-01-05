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
using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.V1
{
    /// <summary>
    ///     An authentication token is used to authenticate with a Neo4j instance. 
    ///     It usually contains a <c>Principal</c>, for instance a username, and one or more <c>Credentials</c>, for instance a password.
    ///     See <see cref="AuthTokens" /> for available types of <see cref="IAuthToken"/>s.
    /// </summary>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, Config)" />
    /// </remarks>
    public interface IAuthToken
    {
    }

    /// <summary>
    ///     This provides methods to create <see cref="IAuthToken"/>s for various authentication schemes supported by this driver. 
    ///     The scheme used must be also supported by the Neo4j instance you are connecting to.
    /// </summary>
    /// <remarks>
    ///     <see cref="GraphDatabase.Driver(string, IAuthToken, Config)" />
    /// </remarks>
    public class AuthTokens
    {
        /// <summary>
        ///     Gets an authentication token that can be used to connect to Neo4j instances with auth disabled.
        ///     This will only work if authentication is disabled on the Neo4j Instance we are connecting to.
        /// </summary>
        /// <remarks>
        ///     <see cref="GraphDatabase.Driver(string, IAuthToken, Config)" />
        /// </remarks>
        public static IAuthToken None => new AuthToken(new Dictionary<string, object> {{"scheme", "none"}});

        /// <summary>
        ///     The basic authentication scheme, using a username and a password.
        /// </summary>
        /// <param name="username">This is the "principal", identifying who this token represents.</param>
        /// <param name="password">This is the "credential", proving the identity of the user.</param>
        /// <returns>An authentication token that can be used to connect to Neo4j.</returns>
        /// <remarks>
        ///     <see cref="GraphDatabase.Driver(string, IAuthToken, Config)" />
        /// </remarks>
        public static IAuthToken Basic(string username, string password)
        {
            return new AuthToken(new Dictionary<string, object>
            {
                {"scheme", "basic"},
                {"principal", username},
                {"credentials", password}
            });
        }
    }
}