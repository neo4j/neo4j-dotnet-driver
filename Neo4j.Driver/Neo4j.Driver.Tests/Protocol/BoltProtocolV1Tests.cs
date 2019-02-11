// Copyright (c) 2002-2019 Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.DiscardAllMessage;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolV1;
using static Neo4j.Driver.Tests.SessionTests;

namespace Neo4j.Driver.Tests.Connector
{
    public class BoltProtocolV1Tests
    {
        public class LoginAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueInitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
                await BoltV1.LoginAsync(mockConn.Object, "user-zhen", AuthTokens.None);

                mockConn.Verify(x => x.EnqueueAsync(It.IsAny<InitMessage>(), It.IsAny<ServerVersionCollector>(), null),
                    Times.Once);
                mockConn.Verify(x => x.SyncAsync());
            }
        }

        public class RunInAutoCommitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueRunAndPullAllAndSend()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IMessageResponseCollector>(),
                        It.IsAny<IRequestMessage>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (msg1, h, msg2) => { h?.DoneSuccess(); });

                await BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null,
                    null);
                mockConn.Verify(x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll),
                    Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                mockConn.Setup(x =>
                        x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (msg1, h, msg2) => { h?.DoneSuccess(); });
                await BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null,
                    null);

                mockConn.Verify(x => x.Server, Times.Once);
            }

            [Fact]
            public async Task ShouldThrowExceptionWhenTxConfigIsUsed()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                var txConfig = new TransactionConfig
                {
                    Timeout = TimeSpan.FromMinutes(1),
                    Metadata = new Dictionary<string, object> {{"key1", "value1"}}
                };

                mockConn.Setup(x =>
                        x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (msg1, h, msg2) => { h?.DoneSuccess(); });

                var error = await Xunit.Record.ExceptionAsync(() =>
                    BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null,
                        txConfig));
                error.Should().BeOfType<ArgumentException>();
                error.Message.Should()
                    .StartWith("Driver is connected to the database that does not support transaction configuration");
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldNotSyncIfBookmarkIsNull()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV1.BeginTransactionAsync(mockConn.Object, null, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll),
                    Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotSyncIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string) null);
                await BoltV1.BeginTransactionAsync(mockConn.Object, bookmark, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll),
                    Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldSyncIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                await BoltV1.BeginTransactionAsync(mockConn.Object, bookmark, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll),
                    Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }

            [Fact]
            public async Task ShouldThrowExceptionIfTxConfigIsGiven()
            {
                var mockConn = new Mock<IConnection>();
                var txConfig = new TransactionConfig
                {
                    Timeout = TimeSpan.FromMinutes(1),
                    Metadata = new Dictionary<string, object> {{"key1", "value1"}}
                };

                var error = await Xunit.Record.ExceptionAsync(() =>
                    BoltV1.BeginTransactionAsync(mockConn.Object, null, txConfig));
                error.Should().BeOfType<ArgumentException>();
                error.Message.Should()
                    .StartWith("Driver is connected to the database that does not support transaction configuration.");
            }
        }

        public class CommitTransactionAsyncMethod
        {
            [Fact]
            public async Task EnqueueCommitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV1.CommitTransactionAsync(mockConn.Object);

                mockConn.Verify(x => x.EnqueueAsync(Commit, It.IsAny<BookmarkCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class RollbackTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueRollbackAndSync()
            {
                var mockConn = new Mock<IConnection>();

                await BoltV1.RollbackTransactionAsync(mockConn.Object);

                mockConn.Verify(x => x.EnqueueAsync(Rollback, It.IsAny<BookmarkCollector>(), DiscardAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class RunInExplicitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldRunPullAllSync()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var statement = new Statement("lalala");

                await BoltV1.RunInExplicitTransactionAsync(mockConn.Object, statement);

                mockConn.Verify(x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll),
                    Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var statement = new Statement("lalala");

                await BoltV1.RunInExplicitTransactionAsync(mockConn.Object, statement);
                mockConn.Verify(x => x.Server, Times.Once);
            }
        }
    }
}