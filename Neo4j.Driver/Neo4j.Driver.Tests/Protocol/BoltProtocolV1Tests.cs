// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.DiscardAllMessage;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolV1;
using static Neo4j.Driver.Tests.SessionTests;

namespace Neo4j.Driver.Tests.Connector
{
    public class BoltProtocolV1Tests
    {
        public class AuthenticateMethod
        {
            [Fact]
            public void ShouldEnqueueInitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
                BoltV1.Authenticate(mockConn.Object, "user-zhen", AuthTokens.None);

                mockConn.Verify(x => x.Enqueue(It.IsAny<InitMessage>(), It.IsAny<ServerVersionCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync());
            }
        }

        public class AuthenticateAsyncMethod
        {
            [Fact]
            public async Task ShouldEnqueueInitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
                await BoltV1.AuthenticateAsync(mockConn.Object, "user-zhen", AuthTokens.None);

                mockConn.Verify(x => x.Enqueue(It.IsAny<InitMessage>(), It.IsAny<ServerVersionCollector>(), null), Times.Once);
                mockConn.Verify(x => x.SyncAsync());
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
                BoltV1.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object, null, null);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<ResultBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Send());
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                BoltV1.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
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

                mockConn.Setup(x => x.Enqueue(It.IsAny<IRequestMessage>(), It.IsAny<IMessageResponseCollector>(), It.IsAny<IRequestMessage>()))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                    (msg1, h, msg2) =>
                    {
                        h?.DoneSuccess();
                    });

                await BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null, null);
                mockConn.Verify(x => x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SendAsync());
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                mockConn.Setup(x => x.Enqueue(It.IsAny<IRequestMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll))
                    .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                        (msg1, h, msg2) =>
                        {
                            h?.DoneSuccess();
                        });
                await BoltV1.RunInAutoCommitTransactionAsync(mockConn.Object, statement, mockHandler.Object, null, null);

                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void ShouldNotSyncIfBookmarkIsNull()
            {
                var mockConn = new Mock<IConnection>();
                BoltV1.BeginTransaction(mockConn.Object, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldNotSyncIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string)null);
                BoltV1.BeginTransaction(mockConn.Object, bookmark);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Never);
            }

            [Fact]
            public void ShouldSyncIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                BoltV1.BeginTransaction(mockConn.Object, bookmark);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldNotSyncIfBookmarkIsNull()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV1.BeginTransactionAsync(mockConn.Object, null);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotSyncIfInvalidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From((string)null);
                await BoltV1.BeginTransactionAsync(mockConn.Object, bookmark);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldSyncIfValidBookmarkGiven()
            {
                var mockConn = new Mock<IConnection>();
                var bookmark = Bookmark.From(FakeABookmark(234));
                await BoltV1.BeginTransactionAsync(mockConn.Object, bookmark);

                mockConn.Verify(x=>x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<IMessageResponseCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class CommitTransactionMethod
        {
            [Fact]
            public void EnqueueCommitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                BoltV1.CommitTransaction(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Commit, It.IsAny<BookmarkCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
            }
        }

        public class CommitTransactionAsyncMethod
        {
            [Fact]
            public async Task EnqueueCommitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                await BoltV1.CommitTransactionAsync(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Commit, It.IsAny<BookmarkCollector>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class RollbackTransactionkMethod
        {
            [Fact]
            public void ShouldEnqueueRollbackAndSync()
            {
                var mockConn = new Mock<IConnection>();
                BoltV1.RollbackTransaction(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Rollback, It.IsAny<BookmarkCollector>(), DiscardAll), Times.Once);
                mockConn.Verify(x => x.Sync(), Times.Once);
            }
        }

        public class RollbackTransactionAsynckMethod
        {
            [Fact]
            public void ShouldEnqueueRollbackAndSync()
            {
                var mockConn = new Mock<IConnection>();
                BoltV1.RollbackTransactionAsync(mockConn.Object);

                mockConn.Verify(x=>x.Enqueue(Rollback, It.IsAny<BookmarkCollector>(), DiscardAll), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
            }
        }

        public class RunInExplicitTransactionMethod
        {
            [Fact]
            public void ShouldRunPullAllSync()
            {
                var mockConn = new Mock<IConnection>();
                var statment = new Statement("lalala");

                BoltV1.RunInExplicitTransaction(mockConn.Object, statment);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<ResultBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.Send(), Times.Once);
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statment = new Statement("lalala");

                BoltV1.RunInExplicitTransaction(mockConn.Object, statment);
                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class RunInExplicitTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldRunPullAllSync()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var statment = new Statement("lalala");

                await BoltV1.RunInExplicitTransactionAsync(mockConn.Object, statment);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<ResultCursorBuilder>(), PullAll), Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }

            [Fact]
            public async Task ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var statment = new Statement("lalala");

                await BoltV1.RunInExplicitTransactionAsync(mockConn.Object, statment);
                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        internal static Mock<IConnection> MockedConnectionWithSuccessResponse()
        {
            var mockConn = new Mock<IConnection>();
            // Whenever you enqueue any message, you immediately receives a response
            mockConn.Setup(x => x.Enqueue(It.IsAny<IRequestMessage>(), It.IsAny<IMessageResponseCollector>(), It.IsAny<IRequestMessage>()))
                .Callback<IRequestMessage, IMessageResponseCollector, IRequestMessage>(
                    (msg1, h, msg2) =>
                    {
                        h?.DoneSuccess();
                        if (msg1 != null)
                        {
                            h?.DoneSuccess();
                        }
                    });
            return mockConn;
        }
    }
}