// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.ExceptionHandling;
using Xunit;

namespace Neo4j.Driver.Tests.Exceptions;

public class Neo4jExceptionFactoryTests
{
    public static IEnumerable<object[]> CodeToTypeMapping = new[]
    {
        new object[] { "Neo.ClientError.Statement.ArgumentError", typeof(StatementArgumentException) },
        new object[] { "Neo.ClientError.Security.Unauthorized", typeof(AuthenticationException) },
        new object[] { "Neo.ClientError.Security.AuthorizationExpired", typeof(AuthorizationException) },
        new object[] { "Neo.ClientError.Database.DatabaseNotFound", typeof(FatalDiscoveryException) },
        new object[] { "Neo.ClientError.Security.Forbidden", typeof(ForbiddenException) },
        new object[] { "Neo.ClientError.Transaction.InvalidBookmark", typeof(InvalidBookmarkException) },
        new object[] { "Neo.ClientError.Transaction.InvalidBookmarkMixture", typeof(InvalidBookmarkMixtureException) },
        new object[] { "Neo.ClientError.Request.Invalid", typeof(ProtocolException) },
        new object[] { "Neo.ClientError.Request.InvalidFormat", typeof(ProtocolException) },
        new object[] { "Neo.ClientError.Security.TokenExpired", typeof(TokenExpiredException) },
        new object[] { "Neo.ClientError.Statement.TypeError", typeof(TypeException) },
        new object[] { "Neo.ClientError.Security.##unknown##", typeof(UnknownSecurityException) }
    };

    [Theory, MemberData(nameof(CodeToTypeMapping))]
    public void ShouldCreateCorrectExceptionType(string code, Type exceptionType)
    {
        var subject = new ClientErrorExceptionFactory();
        var exception = subject.GetException(code, "testmessage");
        exception.Should().BeOfType(exceptionType);
        exception.Should().BeAssignableTo<Neo4jException>();
        if (exception is Neo4jException nEx)
        {
            nEx.Code.Should().Be(code);
            nEx.Message.Should().Be("testmessage");
        }
    }
}
