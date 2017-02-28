// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.IO;
using System.Net.Sockets;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal static class ErrorExtensions
    {
        public static bool IsRecoverableError(this Exception error)
        {
            return error is ClientException || error is TransientException;
        }

        public static bool IsConnectionError(this Exception error)
        {
            return error is IOException || error is SocketException ||
                error.GetBaseException() is IOException || error.GetBaseException() is SocketException;
        }

        public static bool IsClusterError(this Exception error)
        {
            return IsClusterNotALeaderError(error)
                   || IsForbiddenOnReadOnlyDatabaseError(error);
        }

        private static bool IsClusterNotALeaderError(this Exception error)
        {
            return error.HasErrorCode("Neo.ClientError.Cluster.NotALeader");
        }

        private static bool IsForbiddenOnReadOnlyDatabaseError(this Exception error)
        {
            return error.HasErrorCode("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase");
        }

        private static bool HasErrorCode(this Exception error, string code)
        {
            var exception = error as Neo4jException;
            return exception?.Code != null && exception.Code.Equals(code);
        }
    }
}