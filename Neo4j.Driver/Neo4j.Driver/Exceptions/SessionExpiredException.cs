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

using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="SessionExpiredException"/> indicates that the session can no longer satisfy the criteria under which it was acquired,
/// e.g. a server no longer accepts write requests.
///
/// A new session needs to be acquired from the driver and all actions taken on the expired session must be replayed.
/// </summary>
[DataContract]
public class SessionExpiredException : Neo4jException
{
    /// <inheritdoc />
    public override bool IsRetriable => true;

    /// <summary>
    /// Create a new <see cref="SessionExpiredException"/> with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SessionExpiredException(string message) : base(message)
    {
    }

    /// <summary>
    /// Create a new <see cref="SessionExpiredException"/> with an error message and an exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SessionExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
