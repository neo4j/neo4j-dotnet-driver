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

using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>The base class for all Neo4j exceptions.</summary>
[DataContract]
public class Neo4jException : Exception
{
    /// <summary>Create a new <see cref="Neo4jException"/></summary>
    public Neo4jException()
    {
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

    /// <summary>Gets whether the exception retriable or not.</summary>
    public virtual bool IsRetriable => false;

    /// <summary>Gets or sets the code of a Neo4j exception.</summary>
    public string Code { get; set; }
}

/// <summary>
/// A <see cref="ClientException"/> indicates that the client has carried out an operation incorrectly. The error
/// code provided can be used to determine further detail for the problem.
/// </summary>
[DataContract]
public class ClientException : Neo4jException
{
    /// <summary>Create a new <see cref="ClientException"/>.</summary>
    public ClientException()
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ClientException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public ClientException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ClientException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}

/// <summary>
/// A <see cref="TransientException"/> signals a failed operation that may be able to succeed if this operation is
/// retried without any intervention by application-level functionality. The error code provided can be used to determine
/// further details for the problem.
/// </summary>
[DataContract]
public class TransientException : Neo4jException
{
    /// <summary>Create a new <see cref="TransientException"/>.</summary>
    public TransientException()
    {
    }

    /// <summary>Create a new <see cref="TransientException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public TransientException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="TransientException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception which caused this error.</param>
    public TransientException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }

    /// <inheritdoc />
    public override bool IsRetriable => true;
}

/// <summary>
/// A <see cref="DatabaseException"/> indicates that there is a problem within the underlying database. The error
/// code provided can be used to determine further detail for the problem.
/// </summary>
[DataContract]
public class DatabaseException : Neo4jException
{
    /// <summary>Create a new <see cref="DatabaseException"/>.</summary>
    public DatabaseException()
    {
    }

    /// <summary>Create a new <see cref="DatabaseException"/> with an error error message.</summary>
    /// <param name="message">The error message.</param>
    public DatabaseException(string message) : base(string.Empty, message)
    {
    }

    /// <summary>Create a new <see cref="DatabaseException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public DatabaseException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="DatabaseException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception which caused this error.</param>
    public DatabaseException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}

/// <summary>A <see cref="ServiceUnavailableException"/> indicates that the driver cannot communicate with the cluster.</summary>
[DataContract]
public class ServiceUnavailableException : Neo4jException
{
    /// <summary>Create a new <see cref="ServiceUnavailableException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ServiceUnavailableException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ServiceUnavailableException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc />
    public override bool IsRetriable => true;
}

/// <summary>
/// A <see cref="SessionExpiredException"/> indicates that the session can no longer satisfy the criteria under
/// which it was acquired, e.g. a server no longer accepts write requests. A new session needs to be acquired from the
/// driver and all actions taken on the expired session must be replayed.
/// </summary>
[DataContract]
public class SessionExpiredException : Neo4jException
{
    /// <summary>Create a new <see cref="SessionExpiredException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public SessionExpiredException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="SessionExpiredException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SessionExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc />
    public override bool IsRetriable => true;
}

/// <summary>
/// A <see cref="ConnectionReadTimeoutException"/> indicates that the driver timed out trying to read from the
/// network socket.
/// </summary>
[DataContract]
public class ConnectionReadTimeoutException : Neo4jException
{
    /// <summary>Create a new <see cref="ConnectionReadTimeoutException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ConnectionReadTimeoutException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ConnectionReadTimeoutException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionReadTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc />
    public override bool IsRetriable => true;
}

/// <summary>
/// There was a bolt protocol violation of the contract between the driver and the server. When seen this error,
/// contact driver developers.
/// </summary>
[DataContract]
public class ProtocolException : Neo4jException
{
    private const string ErrorCodeInvalid = "Neo.ClientError.Request.Invalid";
    private const string ErrorCodeInvalidFormat = "Neo.ClientError.Request.InvalidFormat";

    /// <summary>Create a new <see cref="ProtocolException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ProtocolException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ProtocolException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public ProtocolException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="SessionExpiredException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }

    internal static bool IsProtocolError(string code)
    {
        return code.Equals(ErrorCodeInvalid) || code.Equals(ErrorCodeInvalidFormat);
    }
}

/// <summary>
/// Failed to connect the driver to the server due to security errors When this type of error happens, recreation
/// of the driver might be required.
/// </summary>
[DataContract]
public class SecurityException : Neo4jException
{
    /// <summary>Create a new <see cref="SecurityException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public SecurityException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="SecurityException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public SecurityException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="SecurityException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SecurityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Failed to authentication the client to the server due to bad credentials To recover from this error, close the
/// current driver and restart with the correct credentials
/// </summary>
[DataContract]
public class AuthenticationException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.Unauthorized";

    /// <summary>Create a new <see cref="AuthenticationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public AuthenticationException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsAuthenticationError(string code)
    {
        return code.Equals(ErrorCode);
    }
}

/// <summary>The authorization information maintained on the server has expired. The client should reconnect.</summary>
public class AuthorizationException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.AuthorizationExpired";

    /// <summary>Create a new <see cref="AuthorizationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public AuthorizationException(string message) : base(ErrorCode, message)
    {
    }

    /// <inheritdoc />
    public override bool IsRetriable => true;

    internal static bool IsAuthorizationError(string code)
    {
        return code.Equals(ErrorCode);
    }
}

/// <summary>
/// The provided token has expired. The current driver instance is considered invalid. It should not be used
/// anymore. The client must create a new driver instance with a valid token.
/// </summary>
public class TokenExpiredException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.TokenExpired";

    /// <summary>Create a new <see cref="TokenExpiredException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public TokenExpiredException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsTokenExpiredError(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}

/// <summary>The provided bookmark is invalid. To recover from this a new session needs to be created.</summary>
public class InvalidBookmarkException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Transaction.InvalidBookmark";

    /// <summary>Create a new <see cref="InvalidBookmarkException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public InvalidBookmarkException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsInvalidBookmarkException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}

/// <summary>The provided bookmark is invalid. To recover from this a new session needs to be created.</summary>
public class InvalidBookmarkMixtureException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Transaction.InvalidBookmarkMixture";

    /// <summary>Create a new <see cref="InvalidBookmarkMixtureException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public InvalidBookmarkMixtureException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsInvalidBookmarkMixtureException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}

/// <summary>A generic argument error has occurred. To recover from this a new session needs to be created.</summary>
[DataContract]
public class ArgumentErrorException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Statement.ArgumentError";

    /// <summary>Create a new <see cref="ArgumentErrorException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ArgumentErrorException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsArgumentErrorException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}

/// <summary>An error occurred related to data typing.</summary>
[DataContract]
public class TypeException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Statement.TypeError";

    /// <summary>Create a new <see cref="TypeException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public TypeException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsTypeException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}

/// <summary>This operation is forbidden.</summary>
[DataContract]
public class ForbiddenException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.Forbidden";

    /// <summary>Create a new <see cref="ForbiddenException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ForbiddenException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsForbiddenException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}

/// <summary>An unknown security error occurred.</summary>
[DataContract]
public class UnknownSecurityException : SecurityException
{
    private const string ErrorCodePrefix = "Neo.ClientError.Security.";

    /// <summary>Create a new <see cref="UnknownSecurityException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    public UnknownSecurityException(string message, string code) : base($"{ErrorCodePrefix}*", message)
    {
        Code = code;
    }

    internal static bool IsUnknownSecurityException(string code)
    {
        return code.StartsWith(ErrorCodePrefix);
    }
}

/// <summary>
/// A value retrieved from the database needs to be truncated for this conversion to work, and will cause working
/// with a modified data.
/// </summary>
[DataContract]
public class ValueTruncationException : ClientException
{
    /// <summary>Create a new <see cref="ValueTruncationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ValueTruncationException(string message) : base(message)
    {
    }
}

/// <summary>
/// A value retrieved from the database cannot be represented with the type to be converted, and will cause
/// working with a modified data.
/// </summary>
[DataContract]
public class ValueOverflowException : ClientException
{
    /// <summary>Create a new <see cref="ValueTruncationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ValueOverflowException(string message) : base(message)
    {
    }
}

/// <summary>
/// There was an error that points us to a fatal problem for routing table discovery, like the requested database
/// could not be found. This kind of errors are identified as non-transient and are not retried.
/// </summary>
[DataContract]
public class FatalDiscoveryException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Database.DatabaseNotFound";

    /// <summary>Create a new <see cref="FatalDiscoveryException"/> with an error code and an error message.</summary>
    /// <param name="message">The error message.</param>
    public FatalDiscoveryException(string message)
        : base(ErrorCode, message)
    {
    }

    internal static bool IsFatalDiscoveryError(string code)
    {
        return code.Equals(ErrorCode);
    }
}

/// <summary>
/// The result has already been consumed either by explicit consume call, or by termination of session or
/// transaction where the result was obtained. Once a result is consumed, the records in the result is not accessible
/// anymore.
/// </summary>
[DataContract]
public class ResultConsumedException : ClientException
{
    /// <summary>Create a new <see cref="ResultConsumedException"/> with an error message</summary>
    /// <param name="message">The error message</param>
    public ResultConsumedException(string message) : base(message)
    {
    }
}

/// <summary>
/// An attempt to BeginTransaction has been made before the sessions existing transaction has been consumed or
/// rolled back. e.g. An attempt to nest transactions has occurred. A session can only have a single transaction at a time.
/// </summary>
[DataContract]
public class TransactionNestingException : ClientException
{
    /// <summary>Create a new <see cref="TransactionNestingException"/> with an error message</summary>
    /// <param name="message">The error message</param>
    public TransactionNestingException(string message) : base(message)
    {
    }
}

/// <summary>
/// The exception that is thrown when calling <see cref="IAsyncTransaction.CommitAsync"/> or
/// <see cref="IAsyncTransaction.RollbackAsync"/> on an <see cref="IAsyncTransaction"/> that has already been closed.
/// </summary>
[DataContract]
public class TransactionClosedException : ClientException
{
    /// <summary>Create a new <see cref="TransactionClosedException"/> with an error message.</summary>
    /// <param name="message">The error message</param>
    public TransactionClosedException(string message) : base(message)
    {
    }
}
