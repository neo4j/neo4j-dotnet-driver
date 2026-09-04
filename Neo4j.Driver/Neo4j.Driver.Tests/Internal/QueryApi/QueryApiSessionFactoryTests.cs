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

#nullable enable

using System;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiSessionFactoryTests
{
    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<QueryApiSessionFactory>();
    private readonly LoggingContextTracker _sessionTracker = new();

    public QueryApiSessionFactoryTests()
    {
        _autoMocker.GetMock<ILoggingContextTracker>()
            .Setup(t => t.CreateChild())
            .Returns(_sessionTracker);

        _autoMocker.GetMock<ISessionIdGenerator>()
            .Setup(g => g.Generate())
            .Returns("session-1");

        _autoMocker.GetMock<ILoggerFactory>()
            .Setup(f => f.GetLoggerForType(It.IsAny<Type>(), It.IsAny<ILoggingContextTracker>()))
            .Returns(new TestLogger(typeof(QueryApiSession)));
    }

    private static SessionConfig SessionConfig()
    {
        return new SessionConfig { DriverContext = TestDriverContext.With(new Uri("http://localhost:7474")) };
    }

    [Fact]
    public void CreateSession_AddsTheSessionIdToTheLoggingContext()
    {
        var subject = _autoMocker.CreateInstance<QueryApiSessionFactory>();

        subject.CreateSession(SessionConfig(), false);

        _sessionTracker.Contexts.Should().ContainSingle(c => c.Key == "session");
    }

    [Fact]
    public void CreateSession_BuildsASession()
    {
        var subject = _autoMocker.CreateInstance<QueryApiSessionFactory>();

        var session = subject.CreateSession(SessionConfig(), false);

        session.Should().NotBeNull();
    }
}
