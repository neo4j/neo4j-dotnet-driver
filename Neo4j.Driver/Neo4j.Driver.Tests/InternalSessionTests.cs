using System;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class InternalSessionTests
    {
        public class RunMethod
        {
            [Fact]
            public void ShouldSyncOnRun()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new InternalSession(null, null, mockConn.Object);
                session.Run("lalalal");

                mockConn.Verify(x => x.Run(It.IsAny<ResultBuilder>(), "lalalal", null), Times.Once);
                mockConn.Verify(x => x.PullAll(It.IsAny<ResultBuilder>()), Times.Once);
                mockConn.Verify(x=>x.Sync());
            }

        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void ShouldNotAllowNewTxWhileOneIsRunning()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new InternalSession(null, null, mockConn.Object);
                session.BeginTransaction();
                var error = Xunit.Record.Exception(() => session.BeginTransaction());
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToOpenTxAfterPreviousIsClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new InternalSession(null, null, mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();
                tx = session.BeginTransaction();
            }

            [Fact]
            public void ShouldNotBeAbleToUseSessionWhileOngoingTransaction()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new InternalSession(null, null, mockConn.Object);
                var tx = session.BeginTransaction();

                var error = Xunit.Record.Exception(() => session.Run("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToUseSessionAgainWhenTransactionIsClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new InternalSession(null, null, mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();

                session.Run("lalal");
            }

            [Fact]
            public void ShouldNotAllowMoreStatementsInSessionWhileConnectionClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(false);
                var session = new InternalSession(null, null, mockConn.Object);

                var error = Xunit.Record.Exception(() => session.Run("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldNotAllowMoreTransactionsInSessionWhileConnectionClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(false);
                var session = new InternalSession(null, null, mockConn.Object);

                var error = Xunit.Record.Exception(() => session.BeginTransaction());
                error.Should().BeOfType<ClientException>();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldDisposeConnOnDispose()
            {
                var mockConn = new Mock<IConnection>();
                var session = new InternalSession(null, null, mockConn.Object);
                session.Dispose();

                mockConn.Verify(x => x.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldDisposeTxAndConnOnDispose()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new InternalSession(null, null, mockConn.Object);
                var tx = session.BeginTransaction();
                session.Dispose();

                mockConn.Verify(x => x.Dispose(), Times.Once);
                mockConn.Verify(x=>x.Run(null, "ROLLBACK", null), Times.Once);

            }
        }
    }
}
