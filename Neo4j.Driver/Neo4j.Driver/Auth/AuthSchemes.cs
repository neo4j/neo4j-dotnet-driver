// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
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

// ReSharper disable once CheckNamespace - public type moved into Auth folder
namespace Neo4j.Driver;

/// <summary>
/// Contains constants for identifying authentication schemes.
/// </summary>
public static class AuthSchemes
{
    /// <summary>
    /// No authentication specified.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// Basic authentication - username and password in plaintext.
    /// </summary>
    public const string Basic = "basic";

    /// <summary>
    /// Kerberos authentication - a string containing a base64 encoded ticket.
    /// </summary>
    public const string Kerberos = "kerberos";

    /// <summary>
    /// Bearer authentication - a string containing a token.
    /// </summary>
    public const string Bearer = "bearer";
}
