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

namespace Neo4j.Driver.Internal.Protocol
{
    public static class BoltProtocolV4Tests
    {
        private static readonly TransactionOptions TxOptions = new TransactionOptions
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
                    {"tx_timeout", TxOptions.Timeout.TotalMilliseconds},
                    {"db", Database},
                    {"tx_metadata", TxOptions.Metadata}
                });
        }

        public class RunInAutoCommitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueRunAndSend()
            {
                var mockConn = NewConnectionWithMode();
                var statement = new Statement("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(),
                        It.IsAny<PullAllMessage>(), It.IsAny<V4.PullResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, h1, msg2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });

                await BoltV4.RunInAutoCommitTransactionAsync(mockConn.Object, statement, true, bookmarkTracker.Object,
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
                var statement = new Statement("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, h1, msg2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });

                await BoltV4.RunInAutoCommitTransactionAsync(mockConn.Object, statement, false, bookmarkTracker.Object,
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
                var statement = new Statement("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, h1, msg2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });

                await BoltV4.RunInAutoCommitTransactionAsync(mockConn.Object, statement, true, bookmarkTracker.Object,
                    resourceHandler.Object, null, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
            }

            [Fact]
            public async Task ShouldPassDatabaseBookmarkAndTxConfigToRunWithMetadataMessage()
            {
                var mockConn = NewConnectionWithMode();
                var statement = new Statement("A cypher query");
                var bookmarkTracker = new Mock<IBookmarkTracker>();
                var resourceHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (m1, h1, m2, h2) =>
                        {
                            h1.OnSuccess(new Dictionary<string, object>());
                            VerifyMetadata(m1.CastOrThrow<RunWithMetadataMessage>().Metadata);
                        });

                await BoltV4.RunInAutoCommitTransactionAsync(mockConn.Object, statement, true, bookmarkTracker.Object,
                    resourceHandler.Object, Database, Bookmark, TxOptions);

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
                var mockConn = SessionTests.MockedConnectionWithSuccessResponse();
                var statement = new Statement("lalala");

                await BoltV4.RunInExplicitTransactionAsync(mockConn.Object, statement, true);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<V4.RunResponseHandler>(), null,
                        null),
                    Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = SessionTests.MockedConnectionWithSuccessResponse();
                var statement = new Statement("lalala");

                await BoltV4.RunInExplicitTransactionAsync(mockConn.Object, statement, true);

                mockConn.Verify(x => x.Server, Times.Once);
            }
        }
    }
}