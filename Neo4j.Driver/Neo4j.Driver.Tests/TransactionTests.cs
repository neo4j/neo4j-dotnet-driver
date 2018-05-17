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
using System.Linq.Expressions;
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
        private static Expression<Action<IConnection>> RunRollback => x => x.Run("ROLLBACK", null, null, false);

        private static Expression<Action<IConnection>> RunCommit => x => x.Run("COMMIT", null,
            It.IsAny<IMessageResponseCollector>(), true);

        private static Expression<Action<IConnection>> RunBegin(IDictionary<string, object> parameters = null)
        {
          return x => x.Run("BEGIN", parameters, null, true);
        }

        public class Constructor
        {
            [Fact]
            public void ShouldRunWithoutBookmarkIfNoBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);
                
                mockConn.Verify(RunBegin(), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            public void ShouldRunWithoutBookmarkIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string)null);
                var tx = new Transaction(mockConn.Object, null, null, bookmark);

                mockConn.Verify(RunBegin(), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldRunWithBookmarkIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                var tx = new Transaction(mockConn.Object, null, null, bookmark);

                IDictionary<string, object> paramters = bookmark.AsBeginTransactionParameters();
                mockConn.Verify(RunBegin(paramters), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldNotKeepInitialBookmark()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                var tx = new Transaction(mockConn.Object, null, null, bookmark);

                tx.Bookmark.Should().BeNull();
            }
        }

        public class SyncBoomarkMethod
        {
            [Fact]
            public void ShouldNotSyncIfBookmakIsNull()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);
                tx.SyncBookmark(null);

                mockConn.Verify(RunBegin(), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldNotSyncIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string)null);
                var tx = new Transaction(mockConn.Object, null, null, bookmark);
                tx.SyncBookmark(bookmark);

                mockConn.Verify(RunBegin(), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldSyncIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                var tx = new Transaction(mockConn.Object, null, null, bookmark);
                tx.SyncBookmark(bookmark);

                IDictionary<string, object> paramters = bookmark.AsBeginTransactionParameters();

                mockConn.Verify(RunBegin(paramters), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
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

        public class RunAsyncMethod
        {
            [Fact]
            public async void ShouldRunPullAllSyncRun()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Run(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IMessageResponseCollector>(), It.IsAny<bool>())).Callback<string, IDictionary<string, object>, IMessageResponseCollector, bool>(
                    (s, d, c, b) =>
                    {
                        c?.DoneSuccess();
                    });
                var tx = new Transaction(mockConn.Object);

                await tx.RunAsync("lalala");

                mockConn.Verify(x => x.Run("lalala", new Dictionary<string, object>(), It.IsAny<ResultCursorBuilder>(), true), Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object);

                try
                {
                    mockConn.Setup(x => x.Run(It.IsAny<string>(), new Dictionary<string, object>(), It.IsAny<ResultCursorBuilder>(), true))
                        .Throws<Neo4jException>();
                    await tx.RunAsync("lalala");
                }
                catch (Neo4jException)
                {
                    // Fine, the state is set to failed now.
                }

                var error = await Xunit.Record.ExceptionAsync(() => tx.RunAsync("ttt"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Run(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IMessageResponseCollector>(), It.IsAny<bool>())).Callback<string, IDictionary<string, object>, IMessageResponseCollector, bool>(
                    (s, d, c, b) =>
                    {
                        c?.DoneSuccess();
                    });
                var tx = new Transaction(mockConn.Object);

                mockConn.Setup(x => x.Run(It.IsAny<string>(), new Dictionary<string, object>(), It.IsAny<ResultCursorBuilder>(), true))
                    .Throws<Neo4jException>();

                var error = await Xunit.Record.ExceptionAsync(() => tx.RunAsync("ttt"));
                error.Should().BeOfType<Neo4jException>();
            }

            [Fact]
            public async void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Run(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IMessageResponseCollector>(), It.IsAny<bool>())).Callback<string, IDictionary<string, object>, IMessageResponseCollector, bool>(
                    (s, d, c, b) =>
                    {
                        c?.DoneSuccess();
                    });

                var tx = new Transaction(mockConn.Object);

                await tx.RunAsync("lalala");

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
                mockConn.Verify(RunCommit, Times.Once);
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
                
                mockConn.Verify(RunRollback, Times.Once);
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
                tx.Dispose();
                mockConn.Verify(RunRollback, Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDispose(), Times.Once);
            }

            [Fact]
            public void ShouldNotReturnConnectionToPoolTwice()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                tx.Dispose();
                tx.Dispose();
                mockHandler.Verify(x => x.OnTransactionDispose(), Times.Once);
            }
        }

        public class CloseAsyncMethod
        {
            [Fact]
            public async void ShouldCommitOnSuccess()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                await tx.CommitAsync();
                mockConn.Verify(RunCommit, Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldRollbackOnFailure()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                await tx.RollbackAsync();
                mockConn.Verify(RunRollback, Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldNotReturnConnectionToPoolTwice()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                await tx.CommitAsync();
                await tx.RollbackAsync();
                mockConn.Verify(RunCommit, Times.Once);
                mockConn.Verify(RunRollback, Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldNotDisposeIfAlreadyClosed()
            {
                var mockConn = new Mock<IConnection>();
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, mockHandler.Object);

                mockConn.ResetCalls();
                await tx.CommitAsync();
                tx.Dispose();
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDispose(), Times.Never);
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

                mockConn.Verify(RunRollback, Times.Never);
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

                mockConn.Verify(RunRollback, Times.Never);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }
        }
    }
}
