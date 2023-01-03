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
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend;
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
        { typeof(Driver.ProtocolException), "ProtocolError" },
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
        { typeof(ArgumentErrorException), "ArgumentError" },
        { typeof(TypeException), "TypeError" },
        { typeof(ForbiddenException), "ForbiddenError" },
        { typeof(UnknownSecurityException), "OtherSecurityException" }
    };

    internal static ProtocolResponse GenerateExceptionResponse(Exception ex)
    {
        var outerExceptionMessage = ex.Message;
        var exceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

        var type = TypeMap.GetValueOrDefault(ex.GetType());

        //if (ex is Neo4jException || ex is NotSupportedException)
        if (type is not null)
        {
            var newError = ProtocolObjectFactory.CreateObject<ProtocolException>();
            newError.ExceptionObj = ex;
            var errorCode = ex is Neo4jException ? ((Neo4jException)ex).Code : type;
            return new ProtocolResponse(
                "DriverError",
                new
                {
                    id = newError.uniqueId,
                    errorType = type,
                    msg = exceptionMessage,
                    code = errorCode
                });
        }

        if (ex is DriverExceptionWrapper)
        {
            var newError = ProtocolObjectFactory.CreateObject<ProtocolException>();
            return new ProtocolResponse(
                "DriverError",
                new
                {
                    id = newError.uniqueId,
                    errorType = ex.InnerException.GetType().Name,
                    msg = exceptionMessage
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

        Trace.WriteLine(
            $"Exception thrown {outerExceptionMessage}\n     which contained -- {exceptionMessage}\n{ex.StackTrace}");

        return new ProtocolResponse("BackendError", new { msg = exceptionMessage });
    }
}
