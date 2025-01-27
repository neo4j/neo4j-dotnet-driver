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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Connector;

public class SocketConnectionTests
{
    private static IAuthToken AuthToken => AuthTokens.None;
    private static string UserAgent => Config.DefaultUserAgent;
    private static ILogger Logger => new Mock<ILogger>().Object;
    private static Uri uri => new("http://neo4j.com");
    private static ServerInfo Server => new(uri);
    private static ISocketClient SocketClient => new Mock<ISocketClient>().Object;

    internal static SocketConnection NewSocketConnection(
        ISocketClient socketClient = null,
        IResponsePipeline pipeline = null,
        ServerInfo server = null,
        ILogger logger = null,
        IBoltProtocolFactory boltProtocolFactory = null,
        DriverContext context = null)
    {
        socketClient ??= SocketClient;
        server ??= Server;
        return new SocketConnection(
            socketClient,
            AuthToken,
            logger ?? Logger,
            server,
            pipeline,
            AuthTokenManagers.None,
            boltProtocolFactory,
            context);
    }

    public class InitMethod
    {
        [Fact]
        public async Task ShouldConnectClient()
        {
            // Given
            var mockClient = new Mock<ISocketClient>();
            mockClient.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);

            var protocolMock = new Mock<IBoltProtocol>();
            var bpFactory = new Mock<IBoltProtocolFactory>();
            bpFactory.Setup(x => x.ForVersion(BoltProtocolVersion.V3_0)).Returns(protocolMock.Object);

            var conn = NewSocketConnection(
                mockClient.Object,
                boltProtocolFactory: bpFactory.Object,
                context: new DriverContext(new Uri("bolt://localhost:7687"), AuthTokenManagers.None, new Config()));

            // When
            await conn.InitAsync();

            // Then
            mockClient.Verify(c => c.ConnectAsync(CancellationToken.None), Times.Once);
            protocolMock.Verify(
                x => x.AuthenticateAsync(
                    conn,
                    It.IsAny<string>(),
                    It.IsAny<IAuthToken>(),
                    It.IsAny<INotificationsConfig>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowClientErrorIfFailedToConnectToServerWithinTimeout()
        {
            // Given
            var mockClient = new Mock<ISocketClient>();
            mockClient.Setup(x => x.ConnectAsync(CancellationToken.None))
                .Throws(new IOException("I will stop socket conn from initialization"));

            // ReSharper disable once ObjectCreationAsStatement
            var conn = new SocketConnection(mockClient.Object, AuthToken, Logger, Server);
            // When
            var error = await Record.ExceptionAsync(() => conn.InitAsync());
            // Then
            error.Should().BeOfType<IOException>();
            error.Message.Should().Be("I will stop socket conn from initialization");
        }
    }

    public class DisposeMethod
    {
        [Fact]
        public async Task StopsTheClient()
        {
            var mock = new Mock<ISocketClient>();
            var con = NewSocketConnection(mock.Object);

            await con.DestroyAsync();

            mock.Verify(c => c.DisposeAsync(), Times.Once);
        }
    }

    public class SyncMethod
    {
        [Fact]
        public async Task DoesNothing_IfMessagesEmpty()
        {
            var mock = new Mock<ISocketClient>();
            var con = NewSocketConnection(mock.Object);

            await con.SyncAsync();

            mock.Verify(c => c.SendAsync(It.IsAny<IEnumerable<IRequestMessage>>()), Times.Never);
        }

        [Fact]
        public async Task SendsMessageAndClearsQueue_WhenMessageOnQueue()
        {
            var mock = new Mock<ISocketClient>();
            var con = NewSocketConnection(mock.Object);

            await con.EnqueueAsync(
                new RunWithMetadataMessage(BoltProtocolVersion.V3_0, new Query("A query"), mode: AccessMode.Read),
                NoOpResponseHandler.Instance);

            await con.SyncAsync();

            mock.Verify(c => c.SendAsync(It.IsAny<IEnumerable<IRequestMessage>>()), Times.Once);
            con.Messages.Count.Should().Be(0);
        }
    }

    public class EnqueueMethod
    {
        [Fact]
        public async Task ShouldEnqueueOneMessage()
        {
            // Given
            var con = NewSocketConnection();

            // When
            await con.EnqueueAsync(
                new RunWithMetadataMessage(BoltProtocolVersion.V3_0, new Query("a query"), mode: AccessMode.Write),
                NoOpResponseHandler.Instance);

            // Then
            con.Messages.Count.Should().Be(1); // Run
            con.Messages[0].Should().BeAssignableTo<RunWithMetadataMessage>();
        }

        [Fact]
        public async Task ShouldEnqueueResultBuilderOnResponseHandler()
        {
            var pipeline = new Mock<IResponsePipeline>();
            var con = NewSocketConnection(pipeline: pipeline.Object);

            await con.EnqueueAsync(
                new RunWithMetadataMessage(BoltProtocolVersion.V4_0, new Query("query"), mode: AccessMode.Read),
                NoOpResponseHandler.Instance);

            pipeline.Verify(h => h.Enqueue(NoOpResponseHandler.Instance), Times.Once);
        }

        [Fact]
        public async Task ShouldEnqueueTwoMessages()
        {
            // Given
            var con = NewSocketConnection();

            // When
            await con.EnqueueAsync(
                new RunWithMetadataMessage(BoltProtocolVersion.V3_0, new Query("a query"), mode: AccessMode.Read),
                NoOpResponseHandler.Instance);

            await con.EnqueueAsync(PullAllMessage.Instance, NoOpResponseHandler.Instance);

            // Then
            con.Messages.Count.Should().Be(2); // Run + PullAll
            con.Messages[0].Should().BeAssignableTo<RunWithMetadataMessage>();
            con.Messages[1].Should().BeAssignableTo<PullAllMessage>();
        }

        [Fact]
        public async Task ShouldEnqueueResultBuildersOnResponseHandler()
        {
            var pipeline = new Mock<IResponsePipeline>();
            var con = NewSocketConnection(pipeline: pipeline.Object);

            await con.EnqueueAsync(
                new RunWithMetadataMessage(BoltProtocolVersion.V3_0, new Query("query"), mode: AccessMode.Read),
                NoOpResponseHandler.Instance);

            await con.EnqueueAsync(PullAllMessage.Instance, NoOpResponseHandler.Instance);
            pipeline.Verify(h => h.Enqueue(NoOpResponseHandler.Instance), Times.Exactly(2));
        }

        [Fact]
        public async Task ShouldEnqueueBoth()
        {
            var pipeline = new Mock<IResponsePipeline>();
            var con = NewSocketConnection(pipeline: pipeline.Object);

            var m1 = new Mock<IRequestMessage>();
            var h1 = new Mock<IResponseHandler>();
            var m2 = new Mock<IRequestMessage>();
            var h2 = new Mock<IResponseHandler>();

            await con.EnqueueAsync(m1.Object, h1.Object, m2.Object, h2.Object);

            con.Messages[0].Should().Be(m1.Object);
            con.Messages[1].Should().Be(m2.Object);
            pipeline.Verify(x => x.Enqueue(It.IsAny<IResponseHandler>()), Times.Exactly(2));
        }
    }

    public class ResetMethod
    {
        [Fact]
        public async Task ShouldDelegateToBoltProtocol()
        {
            var mockClient = new Mock<ISocketClient>();
            var mockProtocol = new Mock<IBoltProtocol>();
            mockClient.Setup(x => x.ConnectAsync(CancellationToken.None));

            var con = NewSocketConnection(mockClient.Object);

            // Should not be done outside of tests.
            con.BoltProtocol = mockProtocol.Object;

            await con.ResetAsync();

            mockProtocol.Verify(x => x.ResetAsync(con), Times.Once);
        }
    }

    public class CloseMethod
    {
        [Fact]
        public async Task ShouldLogoutAndStopAsync()
        {
            // Given
            var mockClient = new Mock<ISocketClient>();
            var conn = NewSocketConnection(mockClient.Object);

            var mockProtocol = new Mock<IBoltProtocol>();
            conn.BoltProtocol = mockProtocol.Object;

            // When
            await conn.CloseAsync();

            // Then
            mockProtocol.Verify(p => p.LogoutAsync(conn));
            mockClient.Verify(c => c.DisposeAsync());
        }

        [Fact]
        public async Task ShouldStopEvenIfFailedToLogoutAsync()
        {
            // Given
            var mockClient = new Mock<ISocketClient>();
            var conn = NewSocketConnection(mockClient.Object);

            var mockProtocol = new Mock<IBoltProtocol>();
            mockProtocol.Setup(x => x.LogoutAsync(It.IsAny<SocketConnection>()))
                .Throws<InvalidOperationException>();

            conn.BoltProtocol = mockProtocol.Object;

            // When
            await conn.CloseAsync();

            // Then
            mockClient.Verify(c => c.DisposeAsync());
        }

        [Fact]
        public async Task ShouldNotThrowExceptionAsync()
        {
            // Given
            var mockClient = new Mock<ISocketClient>();
            mockClient.Setup(x => x.DisposeAsync()).Throws<InvalidOperationException>();

            var mockProtocol = new Mock<IBoltProtocol>();
            mockProtocol.Setup(x => x.LogoutAsync(It.IsAny<SocketConnection>()))
                .Throws<InvalidOperationException>();

            var conn = NewSocketConnection(mockClient.Object);
            conn.BoltProtocol = mockProtocol.Object;

            // When
            await conn.CloseAsync();

            // Then
            mockClient.Verify(c => c.DisposeAsync());
            mockProtocol.Verify(c => c.LogoutAsync(It.IsAny<SocketConnection>()));
        }

        [Fact]
        public async void ShouldNotThrowIfBoltProtocolIsNullAsync()
        {
            // Given
            var mockClient = new Mock<ISocketClient>();
            var conn = NewSocketConnection(mockClient.Object);

            conn.BoltProtocol.Should().BeNull();

            var ex = await Record.ExceptionAsync(() => conn.CloseAsync());

            ex.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(GenerateObjectDisposedExceptions))]
        public async void ShouldNotThrowAndLogIfSocketDisposedAsync(Exception exc)
        {
            // Given
            var logger = new Mock<ILogger>();

            var protocol = new Mock<IBoltProtocol>();
            protocol.Setup(x => x.LogoutAsync(It.IsAny<IConnection>())).ThrowsAsync(exc);

            var mockClient = new Mock<ISocketClient>();
            var conn = NewSocketConnection(mockClient.Object, logger: logger.Object);
            conn.BoltProtocol = protocol.Object;

            var ex = await Record.ExceptionAsync(() => conn.CloseAsync());

            ex.Should().BeNull();
            logger.Verify(x => x.Debug(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
            logger.Verify(
                x => x.Warn(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()),
                Times.Never);
        }

        public static TheoryData<Exception> GenerateObjectDisposedExceptions()
        {
            return new TheoryData<Exception>
            {
                new ObjectDisposedException("socket"),
                new IOException("io", new ObjectDisposedException("socket")),
                new AggregateException(new IOException("io", new ObjectDisposedException("socket")))
            };
        }
    }
}
