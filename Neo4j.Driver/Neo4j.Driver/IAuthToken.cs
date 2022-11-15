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

namespace Neo4j.Driver;

/// <summary>
/// An authentication token is used to authenticate with a Neo4j instance. It usually contains a <c>Principal</c>,
/// for instance a username, and one or more <c>Credentials</c>, for instance a password. See <see cref="AuthTokens" /> for
/// available types of <see cref="IAuthToken" />s.
/// </summary>
/// <remarks>
///     <see
///         cref="GraphDatabase.Driver(string, IAuthToken, System.Action{Neo4j.Driver.ConfigBuilder}(Neo4j.Driver.ConfigBuilder))" />
/// </remarks>
public interface IAuthToken
{
}
