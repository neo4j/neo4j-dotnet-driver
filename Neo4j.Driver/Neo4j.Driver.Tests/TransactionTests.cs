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

using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver;
using Xunit;
using static Neo4j.Driver.Tests.AsyncSessionTests;
using static Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class TransactionTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldObtainProtocolFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                mockConn.Verify(x => x.BoltProtocol);
            }

            [Fact]
            public async Task ShouldSaveBookmark()
            {
                var mockConn = new Mock<IConnection>();
                var mockProtocol = new Mock<IBoltProtocol>();
                mockConn.Setup(x => x.BoltProtocol).Returns(mockProtocol.Object);

                var bookmarks = Bookmarks.From(FakeABookmark(123));
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>(), null, null,
                    bookmarks);

                await tx.BeginTransactionAsync(null);
                mockProtocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), bookmarks,
                        It.IsAny<TransactionConfig>(), null),
                    Times.Once);
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldDelegateToProtocolBeginTxMethod()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionConfig.Default);
                protocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<Bookmarks>(),
                        It.IsAny<TransactionConfig>(), null), Times.Once);
            }
        }

        public class RunAsyncMethod
        {
            [Fact]
            public async void ShouldDelegateToBoltProtocol()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                var query = new Query("lala");
                await tx.RunAsync(query);

                protocol.Verify(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), query, false, It.IsAny<long>()));
            }

            [Fact]
            public async void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                await tx.MarkToClose();

                var error = await ExceptionAsync(() => tx.RunAsync("ttt"));
                error.Should().BeOfType<TransactionClosedException>();
            }

            [Fact]
            public async void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                var query = new Query("lala");

                protocol.Setup(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), query, false, It.IsAny<long>()))
                    .Throws<Neo4jException>();

                var error = await ExceptionAsync(() => tx.RunAsync(query));
                error.Should().BeOfType<Neo4jException>();
            }
        }

        public class CloseAsyncMethod
        {
            [Fact]
            public async void ShouldCommitOnSuccess()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new AsyncTransaction(mockConn.Object, mockHandler.Object);

                mockConn.Invocations.Clear();
                await tx.CommitAsync();

                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx));
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmarks>(), null), Times.Once);
            }

            [Fact]
            public async void ShouldRollbackOnFailure()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new AsyncTransaction(mockConn.Object, mockHandler.Object);

                mockConn.Invocations.Clear();
                await tx.RollbackAsync();
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()));
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmarks>(), null), Times.Once);
            }

            [Fact]
            public async Task ShouldNotDisposeIfAlreadyClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new AsyncTransaction(mockConn.Object, mockHandler.Object);

                mockConn.Invocations.Clear();
                await tx.CommitAsync();
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmarks>(), null), Times.Once);
            }
        }

        public class MarkToClosedMethod
        {
            [Fact]
            public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                await tx.MarkToClose();

                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldThrowExceptionToRunAfterMarkToClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                await tx.MarkToClose();

                tx.Awaiting(t => t.RunAsync("should not run")).Should().Throw<ClientException>().Which.Message.Should()
                    .StartWith("Cannot run query in this transaction");
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosedInCommitAsync()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                await tx.MarkToClose();
                tx.Awaiting(t => t.CommitAsync()).Should().Throw<ClientException>().Which.Message.Should()
                    .Contain("Cannot commit this transaction");
                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosedInRollbackAsync()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                await tx.MarkToClose();
                tx.Awaiting(t => t.RollbackAsync()).Should().NotThrow();
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }
        }

        public class IsOpenTests
        {
            [Fact]
            public async Task ShouldBeOpenWhenConstructed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionConfig.Default);

                tx.IsOpen.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldBeOpenWhenRun()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionConfig.Default);
                await tx.RunAsync("RETURN 1");

                tx.IsOpen.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldBeClosedWhenFailed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionConfig.Default);
                await tx.MarkToClose();

                tx.IsOpen.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldBeClosedWhenCommitted()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionConfig.Default);
                await tx.CommitAsync();

                tx.IsOpen.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldBeClosedWhenRollBacked()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionConfig.Default);
                await tx.RollbackAsync();

                tx.IsOpen.Should().BeFalse();
            }
        }
    }
}