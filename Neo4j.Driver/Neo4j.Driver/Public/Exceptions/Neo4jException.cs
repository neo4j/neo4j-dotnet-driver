// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver;

/// <summary>The base class for all Neo4j exceptions.</summary>
[DataContract]
public class Neo4jException : Exception
{
    /// <summary>Create a new <see cref="Neo4jException"/></summary>
    public Neo4jException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jException"/> class using the specified
    /// <see cref="FailureMessage"/>.
    /// </summary>
    /// <param name="failureMessage">The failure message containing error details.</param>
    /// <param name="innerException">The inner exception.</param>
    internal Neo4jException(FailureMessage failureMessage, Exception innerException = null)
        : base(failureMessage.Message, innerException)
    {
        Code = failureMessage.CodeInternal;
        GqlStatus = failureMessage.GqlStatus;
        GqlStatusDescription = failureMessage.GqlStatusDescription;
        GqlClassification = failureMessage.GqlClassification;
        GqlRawClassification = failureMessage.GqlRawClassification;
        GqlDiagnosticRecord = failureMessage.GqlDiagnosticRecord;
    }

    /// <summary>Create a new <see cref="Neo4jException"/> with an error message</summary>
    /// <param name="message">The error message.</param>
    public Neo4jException(string message) : this(null, message)
    {
    }

    /// <summary>Create a new <see cref="Neo4jException"/> with an error code and an error message</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message</param>
    public Neo4jException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Create a new <see cref="Neo4jException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception</param>
    public Neo4jException(string message, Exception innerException)
        : this(null, message, innerException)
    {
    }

    /// <summary>Create a new <see cref="Neo4jException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public Neo4jException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>
    /// Gets whether the exception is retryable or not.  
    /// Important Note: Methods that use Autocommit transactions  
    /// <see cref="Neo4j.Driver.IAsyncSession.RunAsync(string, System.Action{Neo4j.Driver.TransactionConfigBuilder})"/>,  
    /// <see cref="Neo4j.Driver.IAsyncSession.RunAsync(string, System.Collections.Generic.IDictionary{string, object}, System.Action{Neo4j.Driver.TransactionConfigBuilder})"/>,  
    /// and  
    /// <see cref="Neo4j.Driver.IAsyncSession.RunAsync(Neo4j.Driver.Query, System.Action{Neo4j.Driver.TransactionConfigBuilder})"/>  
    /// are not retryable regardless of this value.
    /// </summary>
    public virtual bool IsRetriable => false;

    /// <summary>Gets or sets the code of a Neo4j exception.</summary>
    public string Code { get; set; }

    /// <summary>Gets the GQL status of the exception.</summary>
    public string GqlStatus { get; }

    /// <summary>Gets the GQL status description of the exception.</summary>
    public string GqlStatusDescription { get; }

    /// <summary>Gets the GQL classification of the exception.</summary>
    public string GqlClassification { get; }

    /// <summary>The raw classification as received from the server.</summary>
    public string GqlRawClassification { get; }

    /// <summary>
    /// Gets further information about the status for diagnostic purposes.
    /// </summary>
    public Dictionary<string, object> GqlDiagnosticRecord { get; }

    internal static Neo4jException Create(FailureMessage failureMessage)
    {
        Exception innerException = null;
        if (failureMessage.GqlCause is not null)
        {
            innerException = Create(failureMessage.GqlCause);
        }

        return new Neo4jException(failureMessage, innerException);
    }
}
