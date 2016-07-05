﻿// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketConnectionTests
    {
        private static ILogger Logger => new Mock<ILogger>().Object;

        private static Mock<ISocketClient> MockSocketClient => new Mock<ISocketClient>();

        public class Construction
        {
            [Fact]
            public void ShouldStartClient()
            {
                var mockClient = new Mock<ISocketClient>();
                // ReSharper disable once ObjectCreationAsStatement
                new SocketConnection(mockClient.Object, AuthTokens.None, Logger, null);

                mockClient.Verify(c => c.Start(), Times.Once);
            }

            [Fact]
            public void ShouldSyncInitMessageImmediately()
            {
                var mockClient = new Mock<ISocketClient>();
                var mockHandler = new Mock<IMessageResponseHandler>();
                new SocketConnection(mockClient.Object, AuthTokens.None, Logger, mockHandler.Object);

                mockHandler.Verify(h => h.Register(It.IsAny<InitMessage>(), null));

                mockClient.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()), Times.Once);
                mockClient.Verify(c => c.Receive(mockHandler.Object, 0), Times.Once);
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfSocketClientIsNull()
            {
                var exception = Record.Exception(() => new SocketConnection(null, AuthTokens.None, Logger));
                exception.Should().NotBeNull();
                exception.Should().BeOfType<ArgumentNullException>();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void StopsTheClient()
            {
                var mock = new Mock<ISocketClient>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, null);

                con.Dispose();
                mock.Verify(c => c.Stop(), Times.Once);
            }
        }

        public class SyncMethod
        {
            [Fact]
            public void DoesNothing_IfMessagesEmpty()
            {
                var mock = new Mock<ISocketClient>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, null);

                con.Sync();
                mock.Reset();
                con.Sync();
                mock.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()),
                    Times.Never);
            }

            [Fact]
            public void SendsMessageAndClearsQueue_WhenMessageOnQueue()
            {
                var mock = new Mock<ISocketClient>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, null);

                con.Sync();
                mock.Verify(c => c.Send(It.IsAny<IEnumerable<IRequestMessage>>()),
                    Times.Once);
                con.Messages.Count.Should().Be(0);
            }
        }

        public class RunMethod
        {
            [Fact]
            public void ShouldEnqueueRunMessage()
            {
                // Given
                var mock = MockSocketClient;
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, null);

                // When
                con.Run(new ResultBuilder(), "a statement");

                // Then
                con.Messages.Count.Should().Be(1); // Run
                con.Messages[0].Should().BeAssignableTo<RunMessage>();
            }

            [Fact]
            public void ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, mockResponseHandler.Object);

                var rb = new ResultBuilder();
                con.Run(rb, "statement");

                mockResponseHandler.Verify(h => h.Register(It.IsAny<RunMessage>(), rb), Times.Once);
            }
        }

        public class PullAllMethod
        {
            [Fact]
            public void ShouldEnqueuedPullAllMessage()
            {
                // Given
                var mock = new Mock<ISocketClient>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, null);

                // When
                con.PullAll(new ResultBuilder());

                // Then
                con.Messages.Count.Should().Be(1); // PullAll
                con.Messages[0].Should().BeAssignableTo<PullAllMessage>();
            }

            [Fact]
            public void ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, mockResponseHandler.Object);

                var rb = new ResultBuilder();
                con.PullAll(rb);

                mockResponseHandler.Verify(h => h.Register(It.IsAny<PullAllMessage>(), rb), Times.Once);
            }
        }

        public class ResetMethod
        {
            [Fact]
            public void ShouldNotClearMessagesResponseHandlerAndEnqueueResetMessage()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, mockResponseHandler.Object);

                con.Run(null, "bula");
                con.Reset();
                var messages = con.Messages;
                messages.Count.Should().Be(2);
                messages[0].Should().BeOfType<RunMessage>();
                messages[1].Should().BeOfType<ResetMessage>();
                mockResponseHandler.Verify(x => x.Register(It.IsAny<RunMessage>(), null), Times.Once);
                mockResponseHandler.Verify(x => x.Register(It.IsAny<ResetMessage>(), null), Times.Once);
            }
        }

        public class HasUnrecoverableError
        {
            [Fact]
            public void ShouldReportErrorIfIsTransientException()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, mockResponseHandler.Object);

                mockResponseHandler.Setup(x => x.Error).Returns(new TransientException("BLAH", "lalala"));
                con.HasUnrecoverableError.Should().BeFalse();
            }

            [Fact]
            public void ShouldReportErrorIfIsDatabaseException()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, mockResponseHandler.Object);

                mockResponseHandler.Setup(x => x.Error).Returns(new DatabaseException("BLAH", "lalala"));
                con.HasUnrecoverableError.Should().BeTrue();
            }

            [Fact]
            public void ShouldNotReportErrorIfIsOtherExceptions()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, AuthTokens.None, Logger, mockResponseHandler.Object);

                mockResponseHandler.Setup(x => x.Error).Returns(new ClientException("BLAH", "lalala"));
                con.HasUnrecoverableError.Should().BeFalse();
            }
        }
    }
}