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

using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.TestKitBackend.Errors;

internal interface IExceptionTypeMapper
{
    string Map(Exception exception);
}

internal class ExceptionTypeMapper : IExceptionTypeMapper
{
    public string Map(Exception exception)
    {
        return exception switch
        {
            ReauthException => "UnsupportedFeatureException",
            TokenExpiredException => "ClientError",
            TransientException => "DriverError",
            TimeoutException => "DriverError",
            AuthorizationException => "AuthorizationExpired",
            UnknownSecurityException => "OtherSecurityException",
            TransactionClosedException => "ClientError",
            TransactionNestingException => nameof(TransactionNestingException),
            NotSupportedException => nameof(NotSupportedException),
            StatementArgumentException => "ArgumentError",
            UnsupportedFeatureException => nameof(UnsupportedFeatureException),
            ObjectDisposedException => nameof(ObjectDisposedException),
            ArgumentException => "ArgumentError",
            InvalidOperationException => nameof(InvalidOperationException),
            _ => exception.GetType().Name.Replace("Exception", "Error")
        };
    }
}
