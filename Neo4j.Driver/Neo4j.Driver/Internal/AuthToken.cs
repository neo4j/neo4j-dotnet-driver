// Copyright (c) 2002-2016 "Neo Technology,"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    /// <summary>
    ///     A simple common token for authentication schemes that easily convert to an auth token map
    /// </summary>
    internal class AuthToken : IAuthToken
    {
        public AuthToken(IDictionary<string, object> content)
        {
            Content = content;
        }

        public IDictionary<string, object> Content { get; }
    }

    internal static class AuthTokenExtensions
    {
        public static IDictionary<string, object> AsDictionary(this IAuthToken authToken)
        {
            if (authToken is AuthToken)
            {
                return ((AuthToken) authToken).Content;
            }
            throw new ClientException($"Unknown authentication token, `{authToken}`. Please use one of the supported " +
                                      $"tokens from `{nameof(AuthTokens)}`.");
        }
    }
}