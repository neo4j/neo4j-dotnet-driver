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

using System.Runtime.CompilerServices;

namespace Neo4j.Driver.Bolt.Extensions;

internal static class ExceptionExtensions
{
    extension<T>(T) where T : Exception
    {
        public static void ThrowIf(bool condition, Func<T> exceptionFactory)
        {
            if (condition)
            {
                throw exceptionFactory();
            }
        }
        
        public static void ThrowIf(bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
        {
            Exception.ThrowIf(condition, () => CreateExceptionWithMessage<T>(message ?? "Condition was true"));
        }
    }

    private static Exception CreateExceptionWithMessage<T>(string message) where T : Exception
    {
        var result = typeof(T).GetConstructor([typeof(string)]) is not null
            ? (Exception?)Activator.CreateInstance(typeof(T), message)
            : Activator.CreateInstance<T>();

        return result ?? throw new InvalidOperationException("Could not create exception of type " + typeof(T));
    }
}
