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

namespace Neo4j.Driver
{
    /// <summary>
    /// Controls whether the driver uses TLS encryption for connections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For most use cases, encryption is configured through the URI scheme rather than this enum.
    /// Use <c>bolt+s://</c> or <c>neo4j+s://</c> to require TLS, or <c>bolt+ssc://</c> /
    /// <c>neo4j+ssc://</c> to require TLS while accepting self-signed server certificates.
    /// </para>
    /// <para>
    /// Use <see cref="EncryptionLevel"/> with <see cref="ConfigBuilder.WithEncryptionLevel"/> only
    /// when connecting via a plain <c>bolt://</c> or <c>neo4j://</c> URI and you need to override
    /// the default (unencrypted) behaviour.
    /// </para>
    /// </remarks>
    public enum EncryptionLevel
    {
        /// <summary>
        /// Connections are made without TLS. This is the default when using a plain
        /// <c>bolt://</c> or <c>neo4j://</c> URI.
        /// </summary>
        None,

        /// <summary>
        /// Connections must use TLS. Equivalent to using a <c>+s</c> URI scheme suffix
        /// (e.g. <c>bolt+s://</c> or <c>neo4j+s://</c>).
        /// </summary>
        Encrypted
    }
}