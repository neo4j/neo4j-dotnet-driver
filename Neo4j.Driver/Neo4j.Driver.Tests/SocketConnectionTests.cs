using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SocketConnectionTests
    {
        public class Construction
        {
            [Fact]
            public void ShouldStartClient()
            {
                var mockClient = new Mock<ISocketClient>();
                // ReSharper disable once ObjectCreationAsStatement
                new SocketConnection(mockClient.Object);

                mockClient.Verify(c=>c.Start(), Times.Once);
            }

            [Fact]
            public void ShouldEnqueuesInitMessage()
            {
                var mockClient = new Mock<ISocketClient>();
                var socketConnection = new SocketConnection(mockClient.Object);

                //socketConnection.Init("testclient");
                socketConnection.Messages.Should().HaveCount(1);
                var msg = socketConnection.Messages.First();
                msg.Should().BeAssignableTo<InitMessage>();
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfSocketClientIsNull()
            {
                var exception = Record.Exception(() => new SocketConnection(null));
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
                var con = new SocketConnection(mock.Object);

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
                var con = new SocketConnection(mock.Object);

                con.Sync();
                mock.Reset();
                con.Sync();
                mock.Verify(c => c.Send( It.IsAny<IEnumerable<IMessage>>(), It.IsAny<IMessageResponseHandler>()), Times.Never);
            }

            [Fact]
            public void SendsMessageAndClearsQueue_WhenMessageOnQueue()
            {
                var mock = new Mock<ISocketClient>();
                var con = new SocketConnection(mock.Object);

                con.Sync();
                mock.Verify(c => c.Send(It.IsAny<IEnumerable<IMessage>>(), It.IsAny<IMessageResponseHandler>()), Times.Once);
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
                var con = new SocketConnection(mock.Object);

                // When
                con.Run(new ResultBuilder(), "a statement");

                // Then
                con.Messages.Count.Should().Be(2); // Init + Run
                con.Messages[1].Should().BeAssignableTo<RunMessage>();
                
            }

            [Fact]
            public void ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, mockResponseHandler.Object);

                var rb = new ResultBuilder();
                con.Run(rb, "statement");

                mockResponseHandler.Verify(h => h.Register(It.IsAny<RunMessage>(), rb), Times.Once);
            }

        }

        private static Mock<ISocketClient> MockSocketClient => new Mock<ISocketClient>();

        public class PullAllMethod
        {
            [Fact]
            public void ShouldEnqueuedPullAllMessage()
            {
                // Given
                var mock = new Mock<ISocketClient>();
                var con = new SocketConnection(mock.Object);

                // When
                con.PullAll(new ResultBuilder());

                // Then
                con.Messages.Count.Should().Be(2); // Init + PullAll
                con.Messages[1].Should().BeAssignableTo<PullAllMessage>();
            }

            [Fact]
            public void ShouldEnqueueResultBuilderOnResponseHandler()
            {
                var mock = MockSocketClient;
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new SocketConnection(mock.Object, mockResponseHandler.Object);

                var rb = new ResultBuilder();
                con.PullAll(rb);

                mockResponseHandler.Verify(h => h.Register(It.IsAny<PullAllMessage>(), rb), Times.Once);
            }
        }
    }
}