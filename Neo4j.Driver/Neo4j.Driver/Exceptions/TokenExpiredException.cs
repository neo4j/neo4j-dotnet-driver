// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License"):
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

using Neo4j.Driver.Internal.ExceptionHandling;

namespace Neo4j.Driver;

/// <summary>
/// The provided token has expired. The current driver instance is considered invalid. It should not
/// be used anymore. The client must create a new driver instance with a valid token.
/// </summary>
[ErrorCode("Neo.ClientError.Security.TokenExpired")]
public class TokenExpiredException : SecurityException
{
    /// <summary>
    /// Create a new <see cref="TokenExpiredException"/> with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TokenExpiredException(string message) : base(message)
    {

    }
}
