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
using Neo4j.Driver.Internal.ExceptionHandling;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="TransientException"/> signals a failed operation that may be able to succeed 
/// if this operation is retried without any intervention by application-level functionality. 
/// The error code provided can be used to determine further details for the problem.
/// </summary>
[DataContract]
[ErrorCode("Neo.TransientError.*")]
public class TransientException : Neo4jException
{
    /// <inheritdoc />
    public override bool IsRetriable => true;

    /// <summary>
    /// Create a new <see cref="TransientException"/>.
    /// </summary>
    public TransientException()
    {
    }

    /// <summary>
    /// Create a new <see cref="TransientException"/> with an error code and an error message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public TransientException(string code, string message) : base(code, message)
    {
    }

    /// <summary>
    /// Create a new <see cref="TransientException"/> with an error code, an error message and an exception.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception which caused this error.</param>
    public TransientException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}
