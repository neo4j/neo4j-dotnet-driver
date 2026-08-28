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
using Neo4j.Driver.TestKitBackend.Connection;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Connection;

public class TestkitConnectionHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<TestkitConnectionHandler>();

    [Fact]
    public async Task Runs_the_message_loop_from_the_connection_scope()
    {
        _autoMocker.GetMock<IConnectionIdProvider>()
            .Setup(p => p.GetConnectionId())
            .Returns("testkit-1");

        _autoMocker.Use<Func<TextReader, IConnectionInput>>(_ => _autoMocker.Get<IConnectionInput>());
        _autoMocker.Use<Func<TextWriter, IConnectionOutput>>(_ => _autoMocker.Get<IConnectionOutput>());

        _autoMocker.Use(BuildRootScope());

        var handler = _autoMocker.CreateInstance<TestkitConnectionHandler>();
        var connection = NewConnection();

        await handler.OnConnectedAsync(connection.Object);

        connection.Object.ConnectionId.Should().Be("testkit-1");
        _autoMocker.GetMock<IMessageLoop>().Verify(l => l.RunAsync("testkit-1"), Times.Once);
    }

    private ILifetimeScope BuildRootScope()
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(new LoggingContext()).As<ILoggingContext>();
        builder.RegisterInstance(_autoMocker.Get<IMessageLoop>()).As<IMessageLoop>();
        return builder.Build();
    }

    [Fact]
    public async Task Waits_for_the_previous_connection_to_finish_before_running_the_next()
    {
        var firstStarted = new TaskCompletionSource();
        var firstMayFinish = new TaskCompletionSource();
        var secondStarted = new TaskCompletionSource();

        _autoMocker.GetMock<IConnectionIdProvider>()
            .SetupSequence(p => p.GetConnectionId())
            .Returns("testkit-1")
            .Returns("testkit-2");

        _autoMocker.GetMock<IMessageLoop>()
            .Setup(l => l.RunAsync("testkit-1"))
            .Returns(async () =>
            {
                firstStarted.SetResult();
                await firstMayFinish.Task;
            });

        _autoMocker.GetMock<IMessageLoop>()
            .Setup(l => l.RunAsync("testkit-2"))
            .Returns(() =>
            {
                secondStarted.SetResult();
                return Task.CompletedTask;
            });

        _autoMocker.Use<Func<TextReader, IConnectionInput>>(_ => _autoMocker.Get<IConnectionInput>());
        _autoMocker.Use<Func<TextWriter, IConnectionOutput>>(_ => _autoMocker.Get<IConnectionOutput>());
        _autoMocker.Use(BuildRootScope());

        var handler = _autoMocker.CreateInstance<TestkitConnectionHandler>();

        var first = handler.OnConnectedAsync(NewConnection().Object);
        await firstStarted.Task;

        var second = handler.OnConnectedAsync(NewConnection().Object);
        var settleWindow = Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
        var settled = await Task.WhenAny(secondStarted.Task, settleWindow);

        settled.Should()
            .NotBeSameAs(
                secondStarted.Task,
                "the next connection must not start its message loop until the previous connection has torn down");

        firstMayFinish.SetResult();
        await first;
        await second;

        secondStarted.Task.IsCompleted.Should().BeTrue();
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
