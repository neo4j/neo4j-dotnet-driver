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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Preview.GqlErrors;
using Neo4j.Driver.Tests.TestBackend.Protocol;
using Neo4j.Driver.Tests.TestBackend.Types;

namespace Neo4j.Driver.Tests.TestBackend.Exceptions;
//TransientException = DriverError
//ClientException = ClientError
//All others = BackendError

/*
neo4jException
|  ClientException
|  |   ValueTruncationException
|  |   ValueOverflowException
|  |   FatalDiscoveryException
|  |   ResultConsumedException
|  TransientException
|  DatabaseException
|  ServiceUnavailableException
|  SessionExpiredException
|  ProtocolException
|  SecurityException
|  |   AuthenticationException
*/

internal static class ExceptionManager
{
    private static Dictionary<Type, string> TypeMap { get; } = new()
    {
        { typeof(Neo4jException), "Neo4jError" },
        { typeof(ClientException), "ClientError" },
        { typeof(TransientException), "DriverError" }, //Should maybe Transient error, talk to Peter or Martin
        { typeof(DatabaseException), "DatabaseError" },
        { typeof(ServiceUnavailableException), "ServiceUnavailableError" },
        { typeof(SessionExpiredException), "SessionExpiredError" },
        { typeof(ProtocolException), "ProtocolError" },
        { typeof(SecurityException), "SecurityError" },
        { typeof(AuthenticationException), "AuthenticationError" },
        { typeof(AuthorizationException), "AuthorizationExpired" },
        { typeof(ValueTruncationException), "ValueTruncationError" },
        { typeof(ValueOverflowException), "ValueOverflowError" },
        { typeof(FatalDiscoveryException), "FatalDiscoveryError" },
        { typeof(ResultConsumedException), "ResultConsumedError" },
        { typeof(TransactionNestingException), "TransactionNestingException" },
        { typeof(TokenExpiredException), "ClientError" },
        { typeof(ConnectionReadTimeoutException), "ConnectionReadTimeoutError" },
        { typeof(InvalidBookmarkException), "InvalidBookmarkError" },
        { typeof(TransactionClosedException), "ClientError" },
        { typeof(NotSupportedException), "NotSupportedException" },
        { typeof(ArgumentException), "ArgumentError" },
        { typeof(InvalidBookmarkMixtureException), "InvalidBookmarkMixtureError" },
        { typeof(StatementArgumentException), "ArgumentError" },
        { typeof(TypeException), "TypeError" },
        { typeof(ForbiddenException), "ForbiddenError" },
        { typeof(UnknownSecurityException), "OtherSecurityException" },
        { typeof(ReauthException), "UnsupportedFeatureException" },
        { typeof(TransactionTerminatedException), "TransactionTerminatedError" }
    };

    internal static ProtocolResponse GenerateExceptionResponse(Exception ex)
    {
        var type = TypeMap.GetValueOrDefault(ex.GetType());
        //var exceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

        if (type is not null)
        {
            var newError = ProtocolObjectFactory.CreateObject<Protocol.ProtocolException>();
            newError.ExceptionObj = ex;
            var data = CreateExceptionDictionary(ex, type, ex.Message);
            data["id"] = newError.uniqueId;

            return new ProtocolResponse("DriverError", data);
        }

        if (ex is DriverExceptionWrapper)
        {
            var newError = ProtocolObjectFactory.CreateObject<Protocol.ProtocolException>();
            return new ProtocolResponse(
                "DriverError",
                new
                {
                    id = newError.uniqueId,
                    errorType = ex.InnerException?.GetType().Name ?? ex.GetType().Name,
                    msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message,
                    retryable = false
                });
        }

        if (ex is TestKitClientException)
        {
            return new ProtocolResponse(
                "FrontendError",
                new
                {
                    msg = ex.Message
                });
        }

        Trace.WriteLine($"Unhandled exception thrown {ex}");

        return new ProtocolResponse("BackendError", new { msg = ex.Message });
    }

    private static Dictionary<string, object> CreateExceptionDictionary(
        Exception ex,
        string type,
        string exceptionMessage,
        bool isCause = false)
    {
        var ne = ex as Neo4jException;
        var gqlError = ne?.GetGqlErrorPreview();
        var diagnosticRecord = gqlError?.GqlDiagnosticRecord?.ToDictionary(y => y.Key, y => NativeToCypher.Convert(y.Value));
        var data = new Dictionary<string, object>();

        if (!isCause)
        {
            data.OverwriteFrom(
                ("errorType", type),
                ("code", ne?.Code ?? type),
                ("retryable", ne?.IsRetriable ?? false));
        }

        data.OverwriteFrom(
            null,
            ("msg", exceptionMessage),
            ("gqlStatus", gqlError?.GqlStatus),
            ("statusDescription", gqlError?.GqlStatusDescription),
            ("diagnosticRecord", diagnosticRecord),
            ("rawClassification", gqlError?.GqlRawClassification),
            ("classification", gqlError?.GqlClassification));

        if (ne?.InnerException != null)
        {
            var exceptionDictionary = CreateExceptionDictionary(
                ne.InnerException,
                "GqlError",
                ne.InnerException.Message,
                true);

            data["cause"] = new { name = "GqlError", data = exceptionDictionary };
        }

        return data;
    }
}
