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
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Xunit;
using V1 = Neo4j.Driver.Internal.MessageHandling.V1;
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

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<InitMessage>(), It.IsAny<V1.InitResponseHandler>(), null, null),
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
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        async (msg1, h1, msg2, h2) => { await h1?.OnSuccessAsync(new Dictionary<string, object>()); });

                await BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, bookmarkTracker.Object,
                    resourceHandler.Object, null, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.RunResponseHandler>(), PullAll,
                        It.IsAny<V1.PullResponseHandler>()), Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x =>
                        x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.RunResponseHandler>(), PullAll,
                            It.IsAny<V1.PullResponseHandler>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        async (msg1, h1, msg2, h2) => { await h1?.OnSuccessAsync(new Dictionary<string, object>()); });

                await BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, bookmarkTracker.Object,
                    resourceHandler.Object, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
            }

            [Fact]
            public async Task ShouldThrowExceptionWhenTxConfigIsUsed()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();
                var txConfig = new TransactionConfig
                {
                    Timeout = TimeSpan.FromMinutes(1),
                    Metadata = new Dictionary<string, object> {{"key1", "value1"}}
                };

                mockConn.Setup(x =>
                        x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.RunResponseHandler>(), PullAll,
                            It.IsAny<V1.PullResponseHandler>()))
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        async (msg1, h1, msg2, h2) => { await h1?.OnSuccessAsync(new Dictionary<string, object>()); });

                var error = await Xunit.Record.ExceptionAsync(() =>
                    BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, bookmarkTracker.Object,
                        resourceHandler.Object, null, txConfig));

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
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.BeginResponseHandler>(), PullAll,
                        It.IsAny<V1.BeginResponseHandler>()),
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
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.BeginResponseHandler>(), PullAll,
                        It.IsAny<V1.BeginResponseHandler>()),
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
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.BeginResponseHandler>(), PullAll,
                        It.IsAny<V1.BeginResponseHandler>()),
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
                error.Should().BeOfType<ArgumentException>().Which.Message.Should().StartWith(
                    "Driver is connected to the database that does not support transaction configuration.");
            }
        }

        public class CommitTransactionAsyncMethod
        {
            [Fact]
            public async Task EnqueueCommitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                var bookmarkTracker = new Mock<IBookmarkTracker>();

                await BoltV1.CommitTransactionAsync(mockConn.Object, bookmarkTracker.Object);

                mockConn.Verify(
                    x => x.EnqueueAsync(Commit, It.IsAny<V1.CommitResponseHandler>(), DiscardAll,
                        It.IsAny<V1.CommitResponseHandler>()), Times.Once);
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

                mockConn.Verify(
                    x => x.EnqueueAsync(Rollback, It.IsAny<V1.RollbackResponseHandler>(), DiscardAll,
                        It.IsAny<V1.RollbackResponseHandler>()), Times.Once);
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

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunMessage>(), It.IsAny<V1.RunResponseHandler>(), PullAll,
                        It.IsAny<V1.PullResponseHandler>()),
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