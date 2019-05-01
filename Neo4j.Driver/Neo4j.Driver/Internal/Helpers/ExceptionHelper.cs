// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Linq;
using System.Reflection;

namespace Neo4j.Driver.Internal
{
    internal static class ExceptionHelper
    {
        private static readonly TypeInfo ExceptionType = typeof(Exception).GetTypeInfo();

        private static bool HasCause(this Exception exc, Func<Exception, bool> predicate)
        {
            // First check exception itself
            if (predicate(exc))
            {
                return true;
            }

            var innerExceptions = default(Exception[]);

            // Check if this is AggregateException
            if (exc is AggregateException aggExc)
            {
                innerExceptions = aggExc.Flatten().InnerExceptions.ToArray();
            }
            else if (exc.InnerException != null)
            {
                innerExceptions = new[] {exc.InnerException};
            }

            // Now go through inner exceptions
            if (innerExceptions != null)
            {
                foreach (var innerExc in innerExceptions)
                {
                    if (HasCause(innerExc, predicate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasCause<T>(this Exception exc) where T : Exception
        {
            return HasCause(exc, cause => cause is T);
        }
    }
}