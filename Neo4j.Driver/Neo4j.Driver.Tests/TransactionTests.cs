using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class TransactionTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldRunBeginAndDiscardAll()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                mockConn.Verify(x=>x.Run(null, "BEGIN", null), Times.Once);
                mockConn.Verify(x=>x.DiscardAll(), Times.Once);
                tx.Finished.Should().BeFalse();
            }
        }

        public class RunMethod
        {
            [Fact]
            public void ShouldRunPullAllSync()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                tx.Run("lalala");

                mockConn.Verify(x => x.Run(It.IsAny<ResultBuilder>(), "lalala", null), Times.Once);
                mockConn.Verify(x => x.PullAll(It.IsAny<ResultBuilder>()), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
                tx.Finished.Should().BeFalse();
            }

            [Fact]
            public void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                try
                {
                    mockConn.Setup(x => x.Run(It.IsAny<ResultBuilder>(), It.IsAny<string>(), null))
                        .Throws<Neo4jException>();
                    tx.Run("lalala");
                }
                catch (Neo4jException e)
                {
                    // Fine, the state is set to failed now.
                }

                var error = Xunit.Record.Exception(()=>tx.Run("ttt"));
                error.Should().BeOfType<ClientException>();
                tx.Finished.Should().BeFalse();
            }

            [Fact]
            public void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);
                   
                mockConn.Setup(x => x.Run(It.IsAny<ResultBuilder>(), It.IsAny<string>(), null))
                        .Throws<Neo4jException>();

                var error = Xunit.Record.Exception(() => tx.Run("ttt"));
                error.Should().BeOfType<Neo4jException>();
                tx.Finished.Should().BeFalse();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldCommitOnSuccess()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                mockConn.ResetCalls();
                tx.Success();
                tx.Dispose();
                mockConn.Verify(x => x.Run(null, "COMMIT", null), Times.Once);
                mockConn.Verify(x => x.DiscardAll(), Times.Once);
                tx.Finished.Should().BeTrue();
            }

            [Fact]
            public void ShouldRollbackOnFailure()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                mockConn.ResetCalls();
                tx.Success();
                // Even if success is called, but if failure is called afterwards, then we rollback
                tx.Failure();
                tx.Dispose();
                mockConn.Verify(x => x.Run(null, "ROLLBACK", null), Times.Once);
                mockConn.Verify(x => x.DiscardAll(), Times.Once);
                tx.Finished.Should().BeTrue();
            }

            [Fact]
            public void ShouldRollbackOnNoExplicitSuccess()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                mockConn.ResetCalls();
                // Even if success is called, but if failure is called afterwards, then we rollback
                tx.Dispose();
                mockConn.Verify(x => x.Run(null, "ROLLBACK", null), Times.Once);
                mockConn.Verify(x => x.DiscardAll(), Times.Once);
                tx.Finished.Should().BeTrue();
            }
        }

    }
}
