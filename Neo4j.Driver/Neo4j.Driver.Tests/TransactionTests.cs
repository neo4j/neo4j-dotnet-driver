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
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Tests.SessionTests;

namespace Neo4j.Driver.Tests
{
    public class TransactionTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldNotSyncWhenNoBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                mockConn.Verify(x=>x.Run("BEGIN", null, null, true), Times.Once);
                mockConn.Verify(x=>x.Sync(), Times.Never);
            }

//            [Fact]
//            public void ShouldSyncIfBookmarkPresents()
//            {
//                var mockConn = new Mock<IConnection>();
//                var bookmark = Bookmark.From(FakeABookmark(234));
//                var tx = new Transaction(mockConn.Object, null, null, bookmark);
//
//                IDictionary<string, object> paramters = bookmark.AsBeginTransactionParameters();
//
//                mockConn.Verify(x => x.Run("BEGIN", paramters, null, true), Times.Once);
//                mockConn.Verify(x => x.Sync(), Times.Once);
//            }

            [Fact]
            public void ShouldNotKeepInitialBookmark()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                var tx = new Transaction(mockConn.Object, null, null, bookmark);

                tx.Bookmark.Should().BeNull();
            }
        }

        public class RunMethod
        {
            [Fact]
            public void ShouldRunPullAllSyncRun()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                tx.Run("lalala");

                mockConn.Verify(x => x.Run("lalala", new Dictionary<string, object>(), It.IsAny<ResultBuilder>(), true), Times.Once);
                mockConn.Verify(x => x.Send(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                try
                {
                    mockConn.Setup(x => x.Run(It.IsAny<string>(), new Dictionary<string, object>(), It.IsAny<ResultBuilder>(), true))
                        .Throws<Neo4jException>();
                    tx.Run("lalala");
                }
                catch (Neo4jException)
                {
                    // Fine, the state is set to failed now.
                }

                var error = Xunit.Record.Exception(()=>tx.Run("ttt"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);
                   
                mockConn.Setup(x => x.Run(It.IsAny<string>(), new Dictionary<string, object>(), It.IsAny<ResultBuilder>(), true))
                        .Throws<Neo4jException>();

                var error = Xunit.Record.Exception(() => tx.Run("ttt"));
                error.Should().BeOfType<Neo4jException>();
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                tx.Run("lalala");

                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldCommitOnSuccess()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                tx.Success();
                tx.Dispose();
                mockConn.Verify(x => x.Run("COMMIT", null, It.IsAny<IMessageResponseCollector>(), true), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
                mockHandler.Verify(x=>x.OnTransactionDispose(), Times.Once);
            }

            [Fact]
            public void ShouldRollbackOnFailure()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                tx.Success();
                // Even if success is called, but if failure is called afterwards, then we rollback
                tx.Failure();
                tx.Dispose();
                mockConn.Verify(x => x.Run("ROLLBACK", null, null, false), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDispose(), Times.Once);
            }

            [Fact]
            public void ShouldRollbackOnNoExplicitSuccess()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                // Even if success is called, but if failure is called afterwards, then we rollback
                tx.Dispose();
                mockConn.Verify(x => x.Run("ROLLBACK", null, null, false), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDispose(), Times.Once);
            }
        }

        public class MarkToClosedMethod
        {
            [Fact]
            public void ShouldNotAllowMoreMessagesAfterMarkToClosed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);
                mockConn.ResetCalls();

                tx.MarkToClose();

                mockConn.Verify(x => x.Run("ROLLBACK", null, It.IsAny<IMessageResponseCollector>(), It.IsAny<bool>()), Times.Never);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldThrowExceptionToRunAfterMarkToClosed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);
                mockConn.ResetCalls();

                tx.MarkToClose();

                var exception = Xunit.Record.Exception(()=>tx.Run("should not run"));
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Cannot run more statements in this transaction");

                mockConn.Verify(x => x.Run("ROLLBACK", null, It.IsAny<IMessageResponseCollector>(), It.IsAny<bool>()), Times.Never);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }
        }
    }
}
