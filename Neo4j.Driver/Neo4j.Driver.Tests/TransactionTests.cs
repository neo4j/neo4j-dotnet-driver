// Copyright (c) 2002-2019 "Neo4j,"
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
using static Neo4j.Driver.Tests.SessionTests;
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

                var bookmark = Bookmark.From(FakeABookmark(123));
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>(), null, null,
                    bookmark);

                await tx.BeginTransactionAsync(null);
                mockProtocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), bookmark,
                        It.IsAny<TransactionOptions>()),
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

                await tx.BeginTransactionAsync(TransactionOptions.Empty);
                protocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<Bookmark>(),
                        It.IsAny<TransactionOptions>()), Times.Once);
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

                var statement = new Statement("lala");
                await tx.RunAsync(statement);

                protocol.Verify(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), statement, false, It.IsAny<long>()));
            }

            [Fact]
            public async void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                await tx.MarkToClose();

                var error = await ExceptionAsync(() => tx.RunAsync("ttt"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                var statement = new Statement("lala");

                protocol.Setup(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), statement, false, It.IsAny<long>()))
                    .Throws<Neo4jException>();

                var error = await ExceptionAsync(() => tx.RunAsync(statement));
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
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
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
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
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
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }
        }

        public class MarkToClosedMethod
        {
            [Fact]
            public void ShouldNotEnqueueMoreMessagesAfterMarkToClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                tx.MarkToClose();

                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public void ShouldThrowExceptionToRunAfterMarkToClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                tx.MarkToClose();

                tx.Awaiting(t => t.RunAsync("should not run")).Should().Throw<ClientException>().Which.Message.Should()
                    .StartWith("Cannot run statement in this transaction");
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public void ShouldNotEnqueueMoreMessagesAfterMarkToClosedInCommitAsync()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                tx.MarkToClose();
                tx.Awaiting(t => t.CommitAsync()).Should().Throw<ClientException>().Which.Message.Should()
                    .Contain("Cannot commit this transaction");
                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public void ShouldNotEnqueueMoreMessagesAfterMarkToClosedInRollbackAsync()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());
                mockConn.Invocations.Clear();

                tx.MarkToClose();
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

                await tx.BeginTransactionAsync(TransactionOptions.Empty);

                tx.IsOpen.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldBeOpenWhenRun()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionOptions.Empty);
                await tx.RunAsync("RETURN 1");

                tx.IsOpen.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldBeClosedWhenFailed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionOptions.Empty);
                await tx.MarkToClose();

                tx.IsOpen.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldBeClosedWhenCommitted()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionOptions.Empty);
                await tx.CommitAsync();

                tx.IsOpen.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldBeClosedWhenRollBacked()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>());

                await tx.BeginTransactionAsync(TransactionOptions.Empty);
                await tx.RollbackAsync();

                tx.IsOpen.Should().BeFalse();
            }
        }
    }
}