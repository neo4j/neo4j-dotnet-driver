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

namespace Neo4j.Driver.TestKitBackend.Errors;

internal interface IExceptionTypeMapper
{
    string Map(Exception exception);
}

internal class ExceptionTypeMapper : IExceptionTypeMapper
{
    // Exact-type match, not inheritance-aware - a subclass not listed here falls through to the
    // exact-type-name default rather than inheriting its base class's entry.
    private static readonly Dictionary<Type, string> ErrorTypes = new()
    {
        [typeof(Neo4jException)] = "Neo4jError",
        [typeof(ClientException)] = "ClientError",
        [typeof(TransientException)] = "DriverError",
        [typeof(AuthorizationException)] = "AuthorizationExpired",
        [typeof(TokenExpiredException)] = "ClientError",
        [typeof(AuthenticationException)] = "AuthenticationError",
        [typeof(UnknownSecurityException)] = "OtherSecurityException",
        [typeof(ArgumentException)] = "ArgumentError"
    };

    public string Map(Exception exception)
    {
        return ErrorTypes.GetValueOrDefault(exception.GetType(), exception.GetType().Name);
    }
}
