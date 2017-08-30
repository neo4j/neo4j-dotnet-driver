// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using static Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketConnectionTests
    {
        private static IAuthToken AuthToken => AuthTokens.None;
        private static string UserAgent => ConnectionSettings.DefaultUserAgent;
        private static ILogger Logger => new Mock<ILogger>().Object;
        private static IServerInfo Server => new ServerInfo(new Uri("http://neo4j.com"));
        private static ISocketClient SocketClient => new Mock<ISocketClient>().Object;

        internal static SocketConnection NewSocketConnection(ISocketClient socketClient = null, IMessageResponseHandler handler = null, IServerInfo server = null)
        {
            socketClient = socketClient ?? SocketClient;
            server = server ?? Server;
            return new SocketConnection(socketClient, AuthToken, UserAgent, Logger, server, handler);
        }

        public class Construction
        {
            [Fact]
            public void ShouldThrowArgumentNullExceptionIfSocketClientIsNull()
            {
                var exception = Exception(() => new SocketConnection(null, AuthToken, UserAgent, Logger, Server));
                exception.Should().NotBeNull();
                exception.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfAuthTokenIsNull()
            {
                var exception = Exception(() => new SocketConnection(SocketClient, null, UserAgent, Logger, Server));
                exception.Should().NotBeNull();
                exception.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfServerUriIsNull()
            {
                var exception = Exception(() => new SocketConnection(SocketClient, AuthToken, UserAgent, Logger, null));
                exception.Should().NotBeNull();
                exception.Should().BeOfType<ArgumentNullException>();
            }
        }

        public class InitMethod
        {
            [Fact]
            public void ShouldStartClient()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var conn = NewSocketConnection(mockClient.Object);

                // When
                conn.Init();

                // Then
                mockClient.Verify(c => c.StartAsync(), Times.Once);
            }

            [Fact]
            public void ShouldSyncInitMessageImmediately()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                var mockHandler = new Mock<IMessageResponseHandler>();
                mockHandler.Setup(x => x.UnhandledMessageSize).Returns(1);
                var conn = NewSocketConnection(mockClient.Object, mockHandler.Object);

                // When
                conn.Init();

                // Then
                mockHandler.Verify(h => h.EnqueueMessage(It.IsAny<InitMessage>(), It.IsAny<InitCollector>()));

                mockClient.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()), Times.Once);
                mockClient.Verify(c => c.Receive(mockHandler.Object), Times.Once);
            }

            [Fact]
            public void ShouldThrowClientErrorIfFailedToConnectToServerWithinTimeout()
            {
                // Given
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.StartAsync()).Throws(new IOException("I will stop socket conn from initialization"));
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
                mock.Verify(c => c.Dispose(), Times.Once);
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
                con.Run("A statement");

                con.Sync();
                mock.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()),
                    Times.Once);
                con.Messages.Count.Should().Be(0);
            }
        }

        public class RunMethod
        {
            [Fact]
            public void ShouldEnqueueRunMessageAndDiscardAllMessage()
            {
                // Given
                var con = NewSocketConnection();

                // When
                con.Run("a statement", null, new ResultBuilder(), false);

                // Then
                con.Messages.Count.Should().Be(2); // Run + DiscardAll
                con.Messages[0].Should().BeAssignableTo<RunMessage>();
                con.Messages[1].Should().BeAssignableTo<DiscardAllMessage>();
            }

            [Fact]
            public void ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = NewSocketConnection(handler:mockResponseHandler.Object);

                var rb = new ResultBuilder();
                con.Run("statement", null, rb, false);

                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<RunMessage>(), rb), Times.Once);
                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<DiscardAllMessage>(), rb), Times.Once);
            }

            [Fact]
            public void ShouldEnqueueRunMessageAndPullAllMessage()
            {
                // Given
                var con = NewSocketConnection();

                // When
                con.Run("a statement", null, new ResultBuilder(), true);

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

                var rb = new ResultBuilder();
                con.Run("statement", null, rb, true);

                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<RunMessage>(), rb), Times.Once);
                mockResponseHandler.Verify(h => h.EnqueueMessage(It.IsAny<PullAllMessage>(), rb), Times.Once);
            }
        }

        public class ResetMethod
        {
            [Fact]
            public void ShouldNotClearMessagesResponseHandlerAndEnqueueResetMessage()
            {
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = NewSocketConnection(handler: mockResponseHandler.Object);

                con.Run("bula", null, null, false);
                con.Reset();
                var messages = con.Messages;
                messages.Count.Should().Be(3);
                messages[0].Should().BeOfType<RunMessage>();
                messages[1].Should().BeOfType<DiscardAllMessage>();
                messages[2].Should().BeOfType<ResetMessage>();
                mockResponseHandler.Verify(x => x.EnqueueMessage(It.IsAny<RunMessage>(), null), Times.Once);
                mockResponseHandler.Verify(x => x.EnqueueMessage(It.IsAny<DiscardAllMessage>(), null), Times.Once);
                mockResponseHandler.Verify(x => x.EnqueueMessage(It.IsAny<ResetMessage>(), null), Times.Once);
            }
        }
    }
}
