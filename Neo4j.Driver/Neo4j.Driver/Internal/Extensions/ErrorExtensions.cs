// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver;

namespace Neo4j.Driver.Internal
{
    internal static class ErrorExtensions
    {
        public static Neo4jException ParseServerException(string code, string message)
        {
            Neo4jException error;
            var parts = code.Split('.');
            var classification = parts[1].ToLowerInvariant();
            switch (classification)
            {
                case "clienterror":
                    if (AuthenticationException.IsAuthenticationError(code))
                    {
                        error = new AuthenticationException(message);
                    }
					else if(AuthorizationException.IsAuthorizationError(code))
					{
						error = new AuthorizationException(message);
					}
                    else if (SecurityException.IsSecurityException(code))
                    {
                        error = new SecurityException(code, message);
                    }
                    else if (ProtocolException.IsProtocolError(code))
                    {
                        error = new ProtocolException(code, message);
                    }
                    else if (FatalDiscoveryException.IsFatalDiscoveryError(code))
                    {
                        error = new FatalDiscoveryException(message);
                    }
					else if(TokenExpiredException.IsTokenExpiredError(code))
					{
						error = new TokenExpiredException(message);
					}
					else if(InvalidBookmarkException.IsInvalidBookmarkException(code))
					{
						error = new InvalidBookmarkException(message);
					}
                    else
                    {
                        error = new ClientException(code, message);
                    }

                    break;
                case "transienterror":
                    error = new TransientException(code, message);
                    break;
                default:
                    error = new DatabaseException(code, message);
                    break;
            }

            return error;
        }

        public static bool IsRetriableError(this Exception error)
        {
			return error is SessionExpiredException || error.IsRetriableTransientError() ||
				   error is ServiceUnavailableException || error.IsAuthorizationError() ||
				   error is ConnectionReadTimeoutException;
		}

        public static bool IsRetriableTransientError(this Exception error)
        {
            return error is TransientException &&
                   // These error code only happens if the transaction is terminated by client.
                   // We should not retry on these errors
                   !error.HasErrorCode("Neo.TransientError.Transaction.Terminated") &&
                   !error.HasErrorCode("Neo.TransientError.Transaction.LockClientStopped");
        }

        public static bool IsRecoverableError(this Exception error)
        {
            return error is ClientException || error is TransientException;
        }

        public static bool IsConnectionError(this Exception error)
        {
            return error is IOException || error is SocketException ||
                   error.GetBaseException() is IOException || error.GetBaseException() is SocketException;
        }

		public static bool IsAuthorizationError(this Exception error)
		{
			return error is AuthorizationException;
		}

        public static bool IsDatabaseUnavailableError(this Exception error)
        {
            return error.HasErrorCode("Neo.TransientError.General.DatabaseUnavailable");
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

        public static ResultConsumedException NewResultConsumedException()
        {
            return new ResultConsumedException(
                "Cannot access records on this result any more as the result has already been consumed " +
                "or the query runner where the result is created has already been closed.");
        }
    }
}