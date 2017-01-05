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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal static class Neo4jErrorExtensions
    {
        public static bool IsRecoverableError(this Neo4jException error)
        {
            return (error is ClientException || error is TransientException) && !IsClusterError(error);
        }

        public static bool IsClusterError(this Neo4jException error)
        {
            return IsClusterNotALeaderError(error)
                   || IsForbiddenOnReadOnlyDatabaseError(error);
        }

        public static bool IsClusterNotALeaderError(this Neo4jException error)
        {
            return error.Code.Equals("Neo.ClientError.Cluster.NotALeader");
        }

        public static bool IsForbiddenOnReadOnlyDatabaseError(this Neo4jException error)
        {
            return error.Code.Equals("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase");
        }
    }
}