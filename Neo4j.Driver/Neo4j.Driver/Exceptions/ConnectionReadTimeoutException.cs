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
///  A <see cref="ConnectionReadTimeoutException"/> indicates that the driver timed out trying to read from the network socket.
/// </summary>
[DataContract]
public class ConnectionReadTimeoutException : Neo4jException
{
    public override bool IsRetriable => true;

    /// <summary>
    /// Create a new <see cref="ConnectionReadTimeoutException"/> with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConnectionReadTimeoutException(string message) : base(message)
    {
    }

    /// <summary>
    /// Create a new <see cref="ConnectionReadTimeoutException"/> with an error message and an exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionReadTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
