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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiSessionFactoryTests
{
    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<QueryApiSessionFactory>();

    [Fact]
    public async Task CreateSession_DisposesSessionScope_WhenSessionDisposed()
    {
        var sessionScopeMock = new Mock<IResolutionScope>();
        sessionScopeMock.Setup(s => s.Resolve<ILoggingContextTracker>()).Returns(Mock.Of<ILoggingContextTracker>());

        _autoMocker.GetMock<IResolutionScope>()
            .Setup(s => s.CreateChildScope(It.IsAny<Action<IServiceRegistry>>()))
            .Returns(sessionScopeMock.Object);

        AsyncEventHandler? capturedHandler = null;
        var sessionMock = new Mock<IInternalAsyncSession>();
        sessionMock
            .SetupAdd(s => s.Disposed += It.IsAny<AsyncEventHandler>())
            .Callback((AsyncEventHandler h) => capturedHandler = h);

        sessionScopeMock.Setup(s => s.Resolve<IInternalAsyncSession>()).Returns(sessionMock.Object);

        var subject = _autoMocker.CreateInstance<QueryApiSessionFactory>();
        var config = SessionConfig.Builder.WithAuthToken(AuthTokens.Basic("neo4j", "pass")).Build();
        subject.CreateSession(config, false);

        capturedHandler.Should().NotBeNull("factory must subscribe to session.Disposed");
        await capturedHandler!(null, EventArgs.Empty);

        sessionScopeMock.Verify(s => s.DisposeAsync(), Times.Once);
    }
}
