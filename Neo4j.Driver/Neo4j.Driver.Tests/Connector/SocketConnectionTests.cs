// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using static Neo4j.Driver.Internal.Result.NoOperationCollector;
using static Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketConnectionTests
    {
        private static IAuthToken AuthToken => AuthTokens.None;
        private static string UserAgent => ConnectionSettings.DefaultUserAgent;
        private static IDriverLogger Logger => new Mock<IDriverLogger>().Object;
        private static IServerInfo Server => new ServerInfo(new Uri("http://neo4j.com"));
        private static ISocketClient SocketClient => new Mock<ISocketClient>().Object;

        internal static SocketConnection NewSocketConnection(ISocketClient socketClient = null, IMessageResponseHandler handler = null, IServerInfo server = null)
        {
            socketClient = socketClient ?? SocketClient;
            server = server ?? Server;
            return new SocketConnection(socketClient, AuthToken, UserAgent, Logger, server, handler);
        }

        public class InitMethod
        {
            [Fact]
            public void ShouldConnectClient()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var mockProtocol = new Mock<IBoltProtocol>();
                mockClient.Setup(x => x.Connect()).Returns(mockProtocol.Object);
                var conn = NewSocketConnection(mockClient.Object);

                // When
                conn.Init();

                // Then
                mockClient.Verify(c => c.Connect(), Times.Once);
                mockProtocol.Verify(p=>p.Login(conn, It.IsAny<string>(), It.IsAny<IAuthToken>()));
            }

            [Fact]
            public void ShouldThrowClientErrorIfFailedToConnectToServerWithinTimeout()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.Connect()).Throws(new IOException("I will stop socket conn from initialization"));
                // ReSharper disable once ObjectCreationAsStatement
                var conn = new SocketConnection(mockClient.Object, AuthToken, UserAgent, Logger, Server);
                // When
                var error = Exception(()=>conn.Init());
                // Then
                error.Should().BeOfType<IOException>();
                error.Message.Should().Be("I will stop socket conn from initialization");
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void StopsTheClient()
            {
                var mock = new Mock<ISocketClient>();
                var con = NewSocketConnection(mock.Object);

                con.Destroy();
                mock.Verify(c => c.Stop(), Times.Once);
            }
        }

        public class SyncMethod
        {
            [Fact]
            public void DoesNothing_IfMessagesEmpty()
            {
                var mock = new Mock<ISocketClient>();
                var con = NewSocketConnection(mock.Object);

                con.Sync();
                mock.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()),
                    Times.Never);
            }

            [Fact]
            public void SendsMessageAndClearsQueue_WhenMessageOnQueue()
            {
                var mock = new Mock<ISocketClient>();
                var con = NewSocketConnection(mock.Object);
                con.Enqueue(new RunMessage("A statement"));

                con.Sync();
                mock.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()),
                    Times.Once);
                con.Messages.Count.Should().Be(0);
            }
        }

        public class EnqueueMethod
        {
            [Fact]
            public void ShouldEnqueueOneMessage()
            {
                // Given
                var con = NewSocketConnection();

                // When
                con.Enqueue(new RunMessage("a statement"), NoOpResponseCollector);

                // Then
                con.Messages.Count.Should().Be(1); // Run
                con.Messages[0].Should().BeAssignableTo<RunMessage>();
            }

            [Fact]
            public void ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = NewSocketConnection(handler:mockResponseHandler.Object);

                con.Enqueue(new RunMessage("statement"), NoOpResponseCollector);

                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<RunMessage>(), NoOpResponseCollector), Times.Once);
            }

            [Fact]
            public void ShouldEnqueueTwoMessages()
            {
                // Given
                var con = NewSocketConnection();

                // When
                con.Enqueue(new RunMessage("a statement"), NoOpResponseCollector, PullAll);

                // Then
                con.Messages.Count.Should().Be(2); // Run + PullAll
                con.Messages[0].Should().BeAssignableTo<RunMessage>();
                con.Messages[1].Should().BeAssignableTo<PullAllMessage>();
            }

            [Fact]
            public void ShouldEnqueueResultBuildersOnResponseHandler()
            {
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = NewSocketConnection(handler: mockResponseHandler.Object);

                con.Enqueue(new RunMessage("statement"), NoOpResponseCollector, PullAll);

                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<RunMessage>(), NoOpResponseCollector), Times.Once);
                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<PullAllMessage>(), NoOpResponseCollector), Times.Once);
            }
        }

        public class ResetMethod
        {
            [Fact]
            public void ShouldDelegateToBoltProtocol()
            {
                var mockClient = new Mock<ISocketClient>();
                var mockProtocol = new Mock<IBoltProtocol>();
                mockClient.Setup(x => x.Connect()).Returns(mockProtocol.Object);

                var con = NewSocketConnection(mockClient.Object);

                con.Init(); // to assign protocol to connection

                con.Reset();
                mockProtocol.Verify(x => x.Reset(con), Times.Once);
            }
        }

        public class CloseMethod
        {
            [Fact]
            public void ShouldLogoutAndStop()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var conn = NewSocketConnection(mockClient.Object);

                var mockProtocol = new Mock<IBoltProtocol>();
                conn.BoltProtocol = mockProtocol.Object;

                // When
                conn.Close();

                // Then
                mockProtocol.Verify(p=>p.Logout(conn));
                mockClient.Verify(c => c.Stop());
            }

            [Fact]
            public void ShouldStopEvenIfFailedToLogout()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var conn = NewSocketConnection(mockClient.Object);

                var mockProtocol = new Mock<IBoltProtocol>();
                mockProtocol.Setup(x => x.Logout(It.IsAny<SocketConnection>())).Throws<InvalidOperationException>();
                conn.BoltProtocol = mockProtocol.Object;

                // When
                conn.Close();

                // Then
                mockClient.Verify(c => c.Stop());
            }

            [Fact]
            public void ShouldNotThrowException()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.Stop()).Throws<InvalidOperationException>();
                
                var mockProtocol = new Mock<IBoltProtocol>();
                mockProtocol.Setup(x => x.Logout(It.IsAny<SocketConnection>())).Throws<InvalidOperationException>();

                var conn = NewSocketConnection(mockClient.Object);
                conn.BoltProtocol = mockProtocol.Object;

                // When
                conn.Close();

                // Then
                mockClient.Verify(c => c.Stop());
                mockProtocol.Verify(c => c.Logout(It.IsAny<SocketConnection>()));
            }

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
                mockProtocol.Verify(p=>p.LogoutAsync(conn));
                mockClient.Verify(c => c.StopAsync());
            }

            [Fact]
            public async Task ShouldStopEvenIfFailedToLogoutAsync()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var conn = NewSocketConnection(mockClient.Object);

                var mockProtocol = new Mock<IBoltProtocol>();
                mockProtocol.Setup(x => x.LogoutAsync(It.IsAny<SocketConnection>())).Throws<InvalidOperationException>();
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
                mockProtocol.Setup(x => x.LogoutAsync(It.IsAny<SocketConnection>())).Throws<InvalidOperationException>();

                var conn = NewSocketConnection(mockClient.Object);
                conn.BoltProtocol = mockProtocol.Object;

                // When
                await conn.CloseAsync();

                // Then
                mockClient.Verify(c => c.StopAsync());
                mockProtocol.Verify(c => c.LogoutAsync(It.IsAny<SocketConnection>()));
            }
        }
    }
}
