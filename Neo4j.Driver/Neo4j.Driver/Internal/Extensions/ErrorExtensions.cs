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

namespace Neo4j.Driver.Internal;

internal static class ErrorExtensions
{
    private static Neo4jExceptionFactory _exceptionFactory = new();

    public static Neo4jException ParseServerException(string code, string message)
    {
        return _exceptionFactory.GetException(code, message);
    }

    public static bool CanBeRetried(this Exception error)
    {
        return error is Neo4jException { IsRetriable: true };
    }

    public static bool IsRecoverableError(this Exception error)
    {
        return error is ClientException or TransientException;
    }

    public static bool IsConnectionError(this Exception error)
    {
        return error is IOException or SocketException ||
            error.GetBaseException() is IOException or SocketException;
    }

    public static bool HasErrorCode(this Exception error, string errorCode)
    {
        return error is Neo4jException ne && ne.Code == errorCode;
    }

    public static bool IsDatabaseUnavailableError(this Exception error)
    {
        return error.HasErrorCode("Neo.TransientError.General.DatabaseUnavailable");
    }

    public static bool IsClusterError(this Exception error)
    {
        return IsClusterNotALeaderError(error) || IsForbiddenOnReadOnlyDatabaseError(error);
    }

    private static bool IsClusterNotALeaderError(this Exception error)
    {
        return error.HasErrorCode("Neo.ClientError.Cluster.NotALeader");
    }

    private static bool IsForbiddenOnReadOnlyDatabaseError(this Exception error)
    {
        return error.HasErrorCode("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase");
    }

    public static ResultConsumedException NewResultConsumedException()
    {
        return new ResultConsumedException(
            "Cannot access records on this result any more as the result has already been consumed " +
            "or the query runner where the result is created has already been closed.");
    }
}
