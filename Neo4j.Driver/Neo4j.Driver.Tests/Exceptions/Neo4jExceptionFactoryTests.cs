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
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Neo4j.Driver.Internal.ExceptionHandling;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Tests.Exceptions;

public class Neo4jExceptionFactoryTests
{
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static object[][] CodeToTypeMapping =
    [
        ["Neo.ClientError.Statement.ArgumentError", typeof(StatementArgumentException)],
        ["Neo.ClientError.Security.Unauthorized", typeof(AuthenticationException)],
        ["Neo.ClientError.Security.AuthorizationExpired", typeof(AuthorizationException)],
        ["Neo.ClientError.Database.DatabaseNotFound", typeof(FatalDiscoveryException)],
        ["Neo.ClientError.Security.Forbidden", typeof(ForbiddenException)],
        ["Neo.ClientError.Transaction.InvalidBookmark", typeof(InvalidBookmarkException)],
        ["Neo.ClientError.Transaction.InvalidBookmarkMixture", typeof(InvalidBookmarkMixtureException)],
        ["Neo.ClientError.Request.Invalid", typeof(ProtocolException)],
        ["Neo.ClientError.Request.InvalidFormat", typeof(ProtocolException)],
        ["Neo.ClientError.Security.TokenExpired", typeof(TokenExpiredException)],
        ["Neo.ClientError.Statement.TypeError", typeof(TypeException)],
        ["Neo.ClientError.Security.##unknown##", typeof(UnknownSecurityException)],
        ["Neo.DatabaseError.blah", typeof(DatabaseException)],
        ["Neo.TransientError.TemporaryDisabled", typeof(TransientException)]
    ];

    [Theory]
    [MemberData(nameof(CodeToTypeMapping))]
    public void ShouldCreateCorrectExceptionType(string code, Type exceptionType)
    {
        var subject = new Neo4jExceptionFactory();
        var exception = subject.GetException(new FailureMessage(code, "test message"));
        exception.Should().BeOfType(exceptionType);
        exception.Code.Should().Be(code);
        exception.Message.Should().Be("test message");
    }
}
