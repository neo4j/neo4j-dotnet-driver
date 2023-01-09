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

using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// There was an error that points us to a fatal problem for routing table discovery, like the requested database
/// could not be found. This kind of errors are identified as non-transient and are not retried.
/// </summary>
[DataContract]
[ClientErrorCode("Neo.ClientError.Database.DatabaseNotFound")]
public class FatalDiscoveryException : ClientException
{
    /// <summary>
    /// Create a new <see cref="FatalDiscoveryException"/> with an error code and an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FatalDiscoveryException(string message)
        : base(message)
    {
    }
}
