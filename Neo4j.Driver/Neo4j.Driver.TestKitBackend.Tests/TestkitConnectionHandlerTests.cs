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

using System.IO.Pipelines;
using Autofac;
using FluentAssertions;
using Microsoft.AspNetCore.Connections;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Logging;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class TestkitConnectionHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<TestkitConnectionHandler>();

    [Fact]
    public async Task Dispatches_each_request_read_from_the_connection()
    {
        const string json = """{"name":"GetFeatures","data":{}}""";
        var message = Mock.Of<IProtocolMessage>();

        _autoMocker.GetMock<IConnectionIdProvider>()
            .Setup(p => p.GetConnectionId())
            .Returns("testkit-1");

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(json)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IConnectionInputFactory>()
            .Setup(f => f.Create(It.IsAny<TextReader>()))
            .Returns(_autoMocker.Get<IConnectionInput>());

        _autoMocker.GetMock<IConnectionOutputFactory>()
            .Setup(f => f.Create(It.IsAny<TextWriter>()))
            .Returns(_autoMocker.Get<IConnectionOutput>());

        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize(json))
            .Returns(message);

        // OnConnectedAsync resolves the per-connection services from a child scope, and
        // Resolve<T> is an extension method a mock can't intercept - so the root scope is a
        // real (tiny) container seeded with this test's mocks.
        _autoMocker.Use(BuildRootScope());

        var handler = _autoMocker.CreateInstance<TestkitConnectionHandler>();
        var connection = NewConnection();

        await handler.OnConnectedAsync(connection.Object);

        connection.Object.ConnectionId.Should().Be("testkit-1");
        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(message), Times.Once);

        // The response writer is only used directly by the handler for BackendError.
        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(It.IsAny<IProtocolMessage>()), Times.Never);
    }

    private ILifetimeScope BuildRootScope()
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(new LoggingContext()).As<ILoggingContext>();
        builder.RegisterInstance(_autoMocker.Get<IMessageSerializer>()).As<IMessageSerializer>();
        builder.RegisterInstance(_autoMocker.Get<IMessageDispatcher>()).As<IMessageDispatcher>();
        builder.RegisterInstance(_autoMocker.Get<IResponseWriter>()).As<IResponseWriter>();
        return builder.Build();
    }

    private static Mock<ConnectionContext> NewConnection()
    {
        var transport = new Mock<IDuplexPipe>();
        transport.SetupGet(t => t.Input).Returns(new Pipe().Reader);
        transport.SetupGet(t => t.Output).Returns(new Pipe().Writer);

        var connection = new Mock<ConnectionContext>();
        connection.SetupProperty(c => c.ConnectionId);
        connection.SetupGet(c => c.Transport).Returns(transport.Object);
        return connection;
    }
}
