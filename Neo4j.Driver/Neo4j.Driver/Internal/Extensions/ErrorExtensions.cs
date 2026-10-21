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
using System.IO;
using System.Net.Sockets;
using Neo4j.Driver.Internal.ExceptionHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal;

internal static class ErrorExtensions
{
    private static readonly Neo4jExceptionFactory _exceptionFactory = new();

    public static Neo4jException ParseServerException(FailureMessage failureMessage)
    {
        return _exceptionFactory.GetException(failureMessage);
    }

    extension(Exception exception)
    {
        public bool CanBeRetried()
        {
            return exception is Neo4jException { IsRetriable: true };
        }

        /// <summary>
        /// Returns true if the server marked the failure as idempotent — i.e. guaranteed that no state
        /// change occurred, so the request can be safely retried (e.g. admission control rejection).
        /// </summary>
        public bool IsIdempotentFailure()
        {
            return
                exception is Neo4jException { GqlDiagnosticRecord: {} dr } &&
                dr.TryGetValue("_idempotent", out var v) &&
                v is true;
        }

        public bool IsRecoverableError()
        {
            return exception is ClientException or TransientException;
        }

        public bool IsConnectionError()
        {
            return
                exception is IOException or SocketException || 
                exception.GetBaseException() is IOException or SocketException;
        }

        public bool HasErrorCode(string errorCode)
        {
            return exception is Neo4jException ne && ne.Code == errorCode;
        }

        public bool HasServerErrorCode()
        {
            return exception is Neo4jException { Code.Length: > 0 };
        }

        public bool IsDatabaseUnavailableError()
        {
            return exception.HasErrorCode("Neo.TransientError.General.DatabaseUnavailable");
        }

        public bool IsClusterError()
        {
            return IsClusterNotALeaderError(exception) || IsForbiddenOnReadOnlyDatabaseError(exception);
        }

        private bool IsClusterNotALeaderError()
        {
            return exception.HasErrorCode("Neo.ClientError.Cluster.NotALeader");
        }

        private bool IsForbiddenOnReadOnlyDatabaseError()
        {
            return exception.HasErrorCode("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase");
        }
    }

    public static ResultConsumedException NewResultConsumedException()
    {
        return new ResultConsumedException(
            "Cannot access records on this result any more as the result has already been consumed " +
            "or the query runner where the result is created has already been closed.");
    }
}
