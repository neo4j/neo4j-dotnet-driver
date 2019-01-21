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
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using static Neo4j.Driver.Internal.Messaging.V3.CommitMessage;
using static Neo4j.Driver.Internal.Messaging.V3.RollbackMessage;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolV3;
using static Neo4j.Driver.Tests.SessionTests;

namespace Neo4j.Driver.Tests.Connector
{
    public class BoltProtocolV3Tests
    {
        internal static readonly TransactionConfig TxConfig = new TransactionConfig
        {
            Timeout = TimeSpan.FromMinutes(1),
            Metadata = new Dictionary<string, object> {{"key1", "value1"}}
        };
        
        internal static readonly Bookmark Bookmark = Bookmark.From(FakeABookmark(123));

        internal static void VerifyMetadata(IDictionary<string, object> metadata)
        {
            metadata.Should().HaveCount(3).And
                .ContainKeys(new List<string> {"bookmarks", "tx_timeout", "tx_metadata"});
            metadata["bookmarks"].CastOrThrow<string[]>().Should().HaveCount(1).And.Contain("neo4j:bookmark:v1:tx123");
            metadata["tx_timeout"].Should().Be(60000L);
            var txMeta = metadata["tx_metadata"].CastOrThrow<Dictionary<string, object>>();
            txMeta.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("key1", "value1"));
        }
        
        public class LoginMethod
        {
            [Fact]
            public void ShouldEnqueueHelloAndSync()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
                BoltV3.Login(mockConn.Object, "user-zhen", AuthTokens.None);

                mockConn.Verify(x => x.Enqueue(It.IsAny<HelloMessage>(), It.IsAny<ServerVersionCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync());
                mockConn.Verify(x => x.UpdateId(It.IsAny<string>()));
            }
        }

        public class LoginAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueHelloAndSync()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
                await BoltV3.LoginAsync(mockConn.Object, "user-zhen", AuthTokens.None);

                mockConn.Verify(x => x.Enqueue(It.IsAny<HelloMessage>(), It.IsAny<ServerVersionCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync());
                mockConn.Verify(x => x.UpdateId(It.IsAny<string>()));
            }
        }
        
        public class LogoutMethod
        {
            [Fact]
            public void ShouldEnqueueGoodbyeAndSend()
            {
                var mockConn = new Mock<IConnection>();
                BoltV3.Logout(mockConn.Object);

                mockConn.Verify(x => x.Enqueue(It.IsAny<GoodbyeMessage>(), null, null), Times.Once);
                mockConn.Verify(x => x.Send());
            }
        }

        public class LogoutAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueGoodbyeAndSend()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV3.LogoutAsync(mockConn.Object);

                mockConn.Verify(x => x.Enqueue(It.IsAny<GoodbyeMessage>(), null, null), Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }
        }

        public class RunInAutoCommitTransactionMethod
        {        
            [Fact]
            public void ShouldEnqueueRunAndPullAllAndSend()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                BoltV3.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object, null, null);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Send());
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                BoltV3.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
            }
            
            [Fact]
            public void ShouldPassBookmarkAndTxConfigToRunWithMetadataMessage()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultBuilder>(), PullAll))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (m0, r, m1) =>
                        {
                            var msg = m0.CastOrThrow<RunWithMetadataMessage>();
                            VerifyMetadata(msg.Metadata);
                        });
                BoltV3.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object,
                    Bookmark, TxConfig);
                
                mockConn.Verify(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultBuilder>(), PullAll), Times.Once);
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

                mockConn.Setup(x => x.Enqueue(It.IsAny<IRequestMessage>(), It.IsAny<ResultCursorBuilder>(), It.IsAny<IRequestMessage>()))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                    (msg1, h, msg2) =>
                    {
                        h?.DoneSuccess();
                    });

                await BoltV3.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null, null);
                mockConn.Verify(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                mockConn.Setup(x => x.Enqueue(It.IsAny<IRequestMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (msg1, h, msg2) =>
                        {
                            h?.DoneSuccess();
                        });
                await BoltV3.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
            }
            
            [Fact]
            public async Task ShouldPassBookmarkAndTxConfigToRunWithMetadataMessage()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();

                mockConn.Setup(x => x.Enqueue(It.IsAny<IRequestMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (m0, h, m1) =>
                        {
                            h?.DoneSuccess();
                            VerifyMetadata(m0.CastOrThrow<RunWithMetadataMessage>().Metadata);
                        });
                await BoltV3.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object,
                    Bookmark, TxConfig);
                
                mockConn.Verify(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll), Times.Once);
            }
        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void ShouldNotSyncIfBookmarkIsNull()
            {
                var mockConn = new Mock<IConnection>();
                BoltV3.BeginTransaction(mockConn.Object, null, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<IMessageResponseCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldNotSyncIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string)null);
                BoltV3.BeginTransaction(mockConn.Object, bookmark, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<IMessageResponseCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldSyncIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                BoltV3.BeginTransaction(mockConn.Object, bookmark,null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<IMessageResponseCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
            }
            
            [Fact]
            public void ShouldPassBookmarkAndTxConfigToRunWithMetadataMessage()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<ResultBuilder>(), null))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (m0, r, m1) =>
                        {
                            var msg = m0.CastOrThrow<BeginMessage>();
                            VerifyMetadata(msg.Metadata);
                        });
                BoltV3.BeginTransaction(mockConn.Object, Bookmark, TxConfig);
                
                mockConn.Verify(x => x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<ResultBuilder>(), null), Times.Once);
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldNotSyncIfBookmarkIsNull()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV3.BeginTransactionAsync(mockConn.Object, null, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<IMessageResponseCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotSyncIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string)null);
                await BoltV3.BeginTransactionAsync(mockConn.Object, bookmark, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<IMessageResponseCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldSyncIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                await BoltV3.BeginTransactionAsync(mockConn.Object, bookmark, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<IMessageResponseCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
            
            [Fact]
            public async Task ShouldPassBookmarkAndTxConfigToRunWithMetadataMessage()
            {
                var mockConn = new Mock<IConnection>();

                mockConn.Setup(x => x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<ResultBuilder>(), null))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (m0, r, m1) =>
                        {
                            var msg = m0.CastOrThrow<BeginMessage>();
                            VerifyMetadata(msg.Metadata);
                        });
                await BoltV3.BeginTransactionAsync(mockConn.Object, Bookmark, TxConfig);
                mockConn.Verify(x => x.Enqueue(It.IsAny<BeginMessage>(), It.IsAny<ResultBuilder>(), null), Times.Once);
            }
        }

        public class CommitTransactionMethod
        {
            [Fact]
            public void EnqueueCommitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                BoltV3.CommitTransaction(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Commit, It.IsAny<BookmarkCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
            }
        }

        public class CommitTransactionAsyncMethod
        {
            [Fact]
            public async Task EnqueueCommitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV3.CommitTransactionAsync(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Commit, It.IsAny<BookmarkCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class RollbackTransactionMethod
        {
            [Fact]
            public void ShouldEnqueueRollbackAndSync()
            {
                var mockConn = new Mock<IConnection>();
                BoltV3.RollbackTransaction(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Rollback, It.IsAny<BookmarkCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
            }
        }

        public class RollbackTransactionAsyncMethod
        {
            [Fact]
            public void ShouldEnqueueRollbackAndSync()
            {
                var mockConn = new Mock<IConnection>();
                BoltV3.RollbackTransactionAsync(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Rollback, It.IsAny<BookmarkCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class RunInExplicitTransactionMethod
        {
            [Fact]
            public void ShouldRunPullAllSync()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("lalala");

                BoltV3.RunInExplicitTransaction(mockConn.Object, statement);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Send(), Times.Once);
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("lalala");

                BoltV3.RunInExplicitTransaction(mockConn.Object, statement);
                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class RunInExplicitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldRunPullAllSync()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var statement = new Statement("lalala");

                await BoltV3.RunInExplicitTransactionAsync(mockConn.Object, statement);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunWithMetadataMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var statement = new Statement("lalala");

                await BoltV3.RunInExplicitTransactionAsync(mockConn.Object, statement);
                mockConn.Verify(x => x.Server, Times.Once);
            }
        }
    }
}