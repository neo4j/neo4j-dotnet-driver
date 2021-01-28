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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Messaging.V4;
using Neo4j.Driver.Tests;
using Xunit;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolUtils;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolV4;
using V4 = Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.Protocol
{
    public static class BoltProtocolV4Tests
    {
        private static readonly TransactionConfig TxConfig = new TransactionConfig
        {
            Timeout = TimeSpan.FromMinutes(1),
            Metadata = new Dictionary<string, object> {{"key1", "value1"}}
        };

        private static readonly Bookmark Bookmark = Bookmark.From("bookmark-123");
        private static readonly string Database = "my-database";

        private static void VerifyMetadata(IDictionary<string, object> metadata)
        {
            metadata.Should()
                .BeEquivalentTo(new Dictionary<string, object>
                {
                    {"bookmarks", new[] {"bookmark-123"}},
                    {"tx_timeout", TxConfig.Timeout.TotalMilliseconds},
                    {"db", Database},
                    {"tx_metadata", TxConfig.Metadata}
                });
        }

        public class RunInAutoCommitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueRunAndSend()
            {
                var mockConn = NewConnectionWithMode();
                var query = new Query("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();
                var V4 = new BoltProtocolV4();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(),
                        It.IsAny<PullAllMessage>(), It.IsAny<V4.PullResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, h1, msg2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });

                await V4.RunInAutoCommitTransactionAsync(mockConn.Object, query, true, bookmarkTracker.Object,
                    resourceHandler.Object, null, null, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(), null,
                        null), Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }

            [Fact]
            public async Task ShouldEnqueueRunPullAndSendIfNotReactive()
            {
                var mockConn = NewConnectionWithMode();
                var query = new Query("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();
                var V4 = new BoltProtocolV4();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, h1, msg2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });

                await V4.RunInAutoCommitTransactionAsync(mockConn.Object, query, false, bookmarkTracker.Object,
                    resourceHandler.Object, null, null, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(),
                        It.IsAny<PullMessage>(), It.IsAny<V4.PullResponseHandler>()), Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = NewConnectionWithMode();
                var query = new Query("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();
                var V4 = new BoltProtocolV4();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, h1, msg2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });

                await V4.RunInAutoCommitTransactionAsync(mockConn.Object, query, true, bookmarkTracker.Object,
                    resourceHandler.Object, null, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
            }

            [Fact]
            public async Task ShouldPassDatabaseBookmarkAndTxConfigToRunWithMetadataMessage()
            {
                var mockConn = NewConnectionWithMode();
                var query = new Query("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();
                var V4 = new BoltProtocolV4();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (m1, h1, m2, h2) =>
                        {
                            h1.OnSuccess(new Dictionary<string, object>());
                            VerifyMetadata(m1.CastOrThrow<RunWithMetadataMessage>().Metadata);
                        });

                await V4.RunInAutoCommitTransactionAsync(mockConn.Object, query, true, bookmarkTracker.Object,
                    resourceHandler.Object, Database, Bookmark, TxConfig);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(), null,
                        null),
                    Times.Once);
            }
        }

        public class RunInExplicitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldRunPullAllSync()
            {
                var mockConn = AsyncSessionTests.MockedConnectionWithSuccessResponse();
                var query = new Query("lalala");
                var V4 = new BoltProtocolV4();

                await V4.RunInExplicitTransactionAsync(mockConn.Object, query, true);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(), null,
                        null),
                    Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = AsyncSessionTests.MockedConnectionWithSuccessResponse();
                var query = new Query("lalala");
                var V4 = new BoltProtocolV4();

                await V4.RunInExplicitTransactionAsync(mockConn.Object, query, true);

                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class ShouldEnqueueAndSyncHello
        {
            private async Task EnqueAndSync(IBoltProtocol V4)
            {
                var mockConn = new Mock<IConnection>();

                mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
                await V4.LoginAsync(mockConn.Object, "user-andy", AuthTokens.None);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<HelloMessage>(), It.IsAny<V4.HelloResponseHandler>(), null, null),
                    Times.Once);
                mockConn.Verify(x => x.SyncAsync());
            }

            [Fact]
            public async Task ShouldEnqueueHelloAndSync()
            {
                var V4 = new BoltProtocolV4();
                await EnqueAndSync(V4);
            }
        }
    }
}