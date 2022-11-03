// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging.V3;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using static Xunit.Record;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketConnectionTests
    {
        private static readonly IResponseHandler NoOpHandler = new NoOpResponseHandler();
        private static IAuthToken AuthToken => AuthTokens.None;
        private static string UserAgent => ConnectionSettings.DefaultUserAgent;
        private static ILogger Logger => new Mock<ILogger>().Object;
        private static IServerInfo Server => new ServerInfo(new Uri("http://neo4j.com"));
        private static ISocketClient SocketClient => new Mock<ISocketClient>().Object;

        internal static SocketConnection NewSocketConnection(ISocketClient socketClient = null,
            IResponsePipeline pipeline = null, IServerInfo server = null, ILogger logger = null)
        {
            socketClient = socketClient ?? SocketClient;
            server = server ?? Server;
            return new SocketConnection(socketClient, AuthToken, UserAgent, logger ?? Logger, server, pipeline);
        }

        public class InitMethod
        {
            [Fact]
            public async Task ShouldConnectClient()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var mockProtocol = new Mock<IBoltProtocol>();
                mockClient.Setup(x => x.ConnectAsync(null, CancellationToken.None)).ReturnsAsync(mockProtocol.Object);
                var conn = NewSocketConnection(mockClient.Object);

                // When
                await conn.InitAsync();

                // Then
                mockClient.Verify(c => c.ConnectAsync(null, CancellationToken.None), Times.Once);
                mockProtocol.Verify(p => 
                    p.LoginAsync(conn, It.IsAny<string>(), It.IsAny<IAuthToken>(),It.IsAny<INotificationFilterConfig[]>()));
            }

            [Fact]
            public async Task ShouldThrowClientErrorIfFailedToConnectToServerWithinTimeout()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.ConnectAsync(null, CancellationToken.None))
                    .Throws(new IOException("I will stop socket conn from initialization"));
                // ReSharper disable once ObjectCreationAsStatement
                var conn = new SocketConnection(mockClient.Object, AuthToken, UserAgent, Logger, Server);
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

                mock.Verify(c => c.StopAsync(), Times.Once);
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

                await con.EnqueueAsync(new RunWithMetadataMessage(new Query("A query"), AccessMode.Read),
                    NoOpHandler);
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
                await con.EnqueueAsync(new RunWithMetadataMessage(new Query("a query"), AccessMode.Write),
                    NoOpHandler);

                // Then
                con.Messages.Count.Should().Be(1); // Run
                con.Messages[0].Should().BeAssignableTo<RunWithMetadataMessage>();
            }

            [Fact]
            public async Task ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var pipeline = new Mock<IResponsePipeline>();
                var con = NewSocketConnection(pipeline: pipeline.Object);

                await con.EnqueueAsync(new RunWithMetadataMessage(new Query("query"), AccessMode.Read),
                    NoOpHandler);

                pipeline.Verify(h => h.Enqueue(It.IsAny<RunWithMetadataMessage>(), NoOpHandler), Times.Once);
            }

            [Fact]
            public async Task ShouldEnqueueTwoMessages()
            {
                // Given
                var con = NewSocketConnection();

                // When
                await con.EnqueueAsync(new RunWithMetadataMessage(new Query("a query"), AccessMode.Read),
                    NoOpHandler, PullAll, NoOpHandler);

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

                await con.EnqueueAsync(new RunWithMetadataMessage(new Query("query"), AccessMode.Read),
                    NoOpHandler, PullAll, NoOpHandler);

                pipeline.Verify(h => h.Enqueue(It.IsAny<RunWithMetadataMessage>(), NoOpHandler),
                    Times.Once);
                pipeline.Verify(h => h.Enqueue(It.IsAny<PullAllMessage>(), NoOpHandler),
                    Times.Once);
            }
        }

        public class ResetMethod
        {
            [Fact]
            public async Task ShouldDelegateToBoltProtocol()
            {
                var mockClient = new Mock<ISocketClient>();
                var mockProtocol = new Mock<IBoltProtocol>();
                mockClient.Setup(x => x.ConnectAsync(null, CancellationToken.None)).ReturnsAsync(mockProtocol.Object);

                var con = NewSocketConnection(mockClient.Object);

                await con.InitAsync(); // to assign protocol to connection
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
                mockClient.Verify(c => c.StopAsync());
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
                mockClient.Verify(c => c.StopAsync());
            }

            [Fact]
            public async Task ShouldNotThrowExceptionAsync()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.StopAsync()).Throws<InvalidOperationException>();

                var mockProtocol = new Mock<IBoltProtocol>();
                mockProtocol.Setup(x => x.LogoutAsync(It.IsAny<SocketConnection>()))
                    .Throws<InvalidOperationException>();

                var conn = NewSocketConnection(mockClient.Object);
                conn.BoltProtocol = mockProtocol.Object;

                // When
                await conn.CloseAsync();

                // Then
                mockClient.Verify(c => c.StopAsync());
                mockProtocol.Verify(c => c.LogoutAsync(It.IsAny<SocketConnection>()));
            }

            [Fact]
            public async void ShouldNotThrowIfBoltProtocolIsNullAsync()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var conn = NewSocketConnection(mockClient.Object);

                conn.BoltProtocol.Should().BeNull();

                var ex = await Xunit.Record.ExceptionAsync(() => conn.CloseAsync());

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

                var ex = await Xunit.Record.ExceptionAsync(() => conn.CloseAsync());

                ex.Should().BeNull();
                logger.Verify(x => x.Debug(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
                logger.Verify(x => x.Warn(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()),
                    Times.Never);
            }

            public static TheoryData<Exception> GenerateObjectDisposedExceptions()
            {
                return new TheoryData<Exception>()
                {
                    new ObjectDisposedException("socket"),
                    new IOException("io", new ObjectDisposedException("socket")),
                    new AggregateException(new IOException("io", new ObjectDisposedException("socket")))
                };
            }
        }
    }
}