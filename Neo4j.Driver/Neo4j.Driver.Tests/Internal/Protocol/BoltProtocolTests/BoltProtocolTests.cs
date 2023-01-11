// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.Protocol.BoltProtocolTests
{
    public class BoltProtocolTests
    {
        public class RunInAutoCommitTransactionAsync
        {
            [Theory]
            [InlineData(4, 3)]
            [InlineData(4, 2)]
            [InlineData(4, 1)]
            [InlineData(4, 0)]
            public async Task ShouldThrowWhenUsingImpersonatedUserWithBoltVersionLessThan44(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));

                var acp = new AutoCommitParams
                {
                    ImpersonatedUser = "Douglas Fir"
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp));

                exception.Should().BeOfType<ArgumentException>();
            }

            [Theory]
            [InlineData(4, 4)]
            [InlineData(5, 0)]
            [InlineData(6, 0)]
            public async Task ShouldNotThrowWhenImpersonatingUserWithBoltVersionGreaterThan43(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var acp = new AutoCommitParams
                {
                    Query = new Query("..."),
                    ImpersonatedUser = "Douglas Fir"
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp));

                exception.Should().BeNull();
            }

            [Fact]
            public async Task ShouldBuildCursor()
            {
                var mockBt = new Mock<IBookmarksTracker>();
                var mockRrh = new Mock<IResultResourceHandler>();

                var acp = new AutoCommitParams
                {
                    Query = new Query("..."),
                    Reactive = false,
                    FetchSize = 10,
                    BookmarksTracker = mockBt.Object,
                    ResultResourceHandler = mockRrh.Object
                };

                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory
                    .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, acp))
                    .Returns(new RunWithMetadataMessage(mockConn.Object.Version, new Query("...")));

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                handlerFactory.Setup(
                        x => x.NewRunResponseHandler(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(
                        new RunResponseHandler(
                            resultCursorBuilderMock.Object,
                            new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")))));

                handlerFactory.Setup(
                        x => x.NewResultCursorBuilder(
                            It.IsAny<SummaryBuilder>(),
                            It.IsAny<IConnection>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, long, Task>>>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, Task>>>(),
                            It.IsAny<IBookmarksTracker>(),
                            It.IsAny<IResultResourceHandler>(),
                            It.IsAny<long>(),
                            It.IsAny<bool>()))
                    .Returns(resultCursorBuilderMock.Object);

                var mockV3 = new Mock<IBoltProtocol>();
                var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);
                await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp);

                msgFactory.Verify(
                    x => x.NewRunWithMetadataMessage(
                        mockConn.Object,
                        acp),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewResultCursorBuilder(
                        It.IsAny<SummaryBuilder>(),
                        mockConn.Object,
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, long, Task>>>(),
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, Task>>>(),
                        mockBt.Object,
                        mockRrh.Object,
                        10,
                        false),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewRunResponseHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<IRequestMessage>(), It.IsNotNull<IResponseHandler>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<RunWithMetadataMessage>(), It.IsNotNull<RunResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SendAsync(), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldEnqueuePullMessageWithReactive()
            {
                var mockBt = new Mock<IBookmarksTracker>();
                var mockRrh = new Mock<IResultResourceHandler>();

                var acp = new AutoCommitParams
                {
                    Query = new Query("..."),
                    Reactive = true,
                    FetchSize = 10,
                    BookmarksTracker = mockBt.Object,
                    ResultResourceHandler = mockRrh.Object
                };

                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory
                    .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, acp))
                    .Returns(new RunWithMetadataMessage(mockConn.Object.Version, new Query("...")));

                msgFactory
                    .Setup(x => x.NewPullMessage(10))
                    .Returns(new PullMessage(10));

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                handlerFactory.Setup(
                        x => x.NewRunResponseHandler(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(
                        new RunResponseHandler(
                            resultCursorBuilderMock.Object,
                            new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")))));

                handlerFactory
                    .Setup(
                        x => x.NewPullResponseHandler(
                            mockBt.Object,
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(
                        new PullResponseHandler(
                            resultCursorBuilderMock.Object,
                            new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0"))),
                            mockBt.Object));

                handlerFactory.Setup(
                        x => x.NewResultCursorBuilder(
                            It.IsAny<SummaryBuilder>(),
                            It.IsAny<IConnection>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, long, Task>>>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, Task>>>(),
                            It.IsAny<IBookmarksTracker>(),
                            It.IsAny<IResultResourceHandler>(),
                            It.IsAny<long>(),
                            It.IsAny<bool>()))
                    .Returns(resultCursorBuilderMock.Object);

                var mockV3 = new Mock<IBoltProtocol>();
                var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);
                await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp);

                msgFactory.Verify(
                    x => x.NewRunWithMetadataMessage(
                        mockConn.Object,
                        acp),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewResultCursorBuilder(
                        It.IsAny<SummaryBuilder>(),
                        mockConn.Object,
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, long, Task>>>(),
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, Task>>>(),
                        mockBt.Object,
                        mockRrh.Object,
                        10,
                        true),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewRunResponseHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()),
                    Times.Exactly(2));

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<RunResponseHandler>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<PullMessage>(), It.IsAny<PullResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SendAsync(), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }
        }

        public class BeginTransactionAsync
        {
            [Theory]
            [InlineData(4, 3)]
            [InlineData(4, 2)]
            [InlineData(4, 1)]
            [InlineData(4, 0)]
            public async Task ShouldThrowWhenUsingImpersonatedUserWithBoltVersionLessThan44(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        "db",
                        Bookmarks.Empty,
                        TransactionConfig.Default,
                        "Douglas Fir"));

                exception.Should().BeOfType<ArgumentException>();
            }

            [Theory]
            [InlineData(4, 4)]
            [InlineData(5, 0)]
            [InlineData(6, 0)]
            public async Task ShouldNotThrowWhenImpersonatingUserWithBoltVersionGreaterThan43(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        "db",
                        Bookmarks.Empty,
                        TransactionConfig.Default,
                        "Douglas Fir"));

                exception.Should().BeNull();
            }

            [Fact]
            public async Task ShouldDelegateLogicToV3()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));
                var bookmarks = new InternalBookmarks();
                var config = new TransactionConfig();

                var mockV3 = new Mock<IBoltProtocol>();
                var protocol = new BoltProtocol(mockV3.Object);

                await protocol.BeginTransactionAsync(
                    mockConn.Object,
                    "db",
                    bookmarks,
                    config,
                    "user");

                mockV3.Verify(
                    x =>
                        x.BeginTransactionAsync(mockConn.Object, "db", bookmarks, config, "user"),
                    Times.Once);

                mockConn.Verify(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<string>(),
                            It.IsAny<Bookmarks>(),
                            It.IsAny<TransactionConfig>(),
                            It.IsAny<string>()),
                    Times.Never);
            }
        }

        public class RunInExplicitTransactionAsync
        {
            [Fact]
            public async Task ShouldBuildCursor()
            {
                var query = new Query("...");
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

                var mockV3 = new Mock<IBoltProtocol>();

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory
                    .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, query))
                    .Returns(new RunWithMetadataMessage(mockConn.Object.Version, new Query("...")));

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                handlerFactory.Setup(
                        x => x.NewRunResponseHandler(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(
                        new RunResponseHandler(
                            resultCursorBuilderMock.Object,
                            new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")))));

                handlerFactory.Setup(
                        x => x.NewResultCursorBuilder(
                            It.IsAny<SummaryBuilder>(),
                            It.IsAny<IConnection>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, long, Task>>>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, Task>>>(),
                            It.IsAny<IBookmarksTracker>(),
                            It.IsAny<IResultResourceHandler>(),
                            It.IsAny<long>(),
                            It.IsAny<bool>()))
                    .Returns(resultCursorBuilderMock.Object);

                var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

                await protocol.RunInExplicitTransactionAsync(mockConn.Object, query, false, 10);

                msgFactory.Verify(
                    x => x.NewRunWithMetadataMessage(mockConn.Object, query),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewResultCursorBuilder(
                        It.IsAny<SummaryBuilder>(),
                        mockConn.Object,
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, long, Task>>>(),
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, Task>>>(),
                        null,
                        null,
                        10,
                        false),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewRunResponseHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<RunResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SendAsync(), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldEnqueuePullForReactive()
            {
                var query = new Query("...");
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory
                    .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, query))
                    .Returns(new RunWithMetadataMessage(mockConn.Object.Version, new Query("...")));

                msgFactory
                    .Setup(x => x.NewPullMessage(10))
                    .Returns(new PullMessage(10));

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                handlerFactory.Setup(
                        x => x.NewRunResponseHandler(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(
                        new RunResponseHandler(
                            resultCursorBuilderMock.Object,
                            new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")))));

                handlerFactory
                    .Setup(
                        x => x.NewPullResponseHandler(
                            null,
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(
                        new PullResponseHandler(
                            resultCursorBuilderMock.Object,
                            new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0"))),
                            null));

                var mockV3 = new Mock<IBoltProtocol>();

                handlerFactory.Setup(
                        x => x.NewResultCursorBuilder(
                            It.IsAny<SummaryBuilder>(),
                            It.IsAny<IConnection>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, long, Task>>>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, Task>>>(),
                            It.IsAny<IBookmarksTracker>(),
                            It.IsAny<IResultResourceHandler>(),
                            It.IsAny<long>(),
                            It.IsAny<bool>()))
                    .Returns(resultCursorBuilderMock.Object);

                var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

                await protocol.RunInExplicitTransactionAsync(mockConn.Object, query, true, 10);

                msgFactory.Verify(
                    x => x.NewRunWithMetadataMessage(mockConn.Object, query),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewResultCursorBuilder(
                        It.IsAny<SummaryBuilder>(),
                        mockConn.Object,
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, long, Task>>>(),
                        It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                            Func<IResultStreamBuilder, long, Task>>>(),
                        null,
                        null,
                        10,
                        true),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewRunResponseHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()),
                    Times.Exactly(2));

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<RunWithMetadataMessage>(), It.IsAny<RunResponseHandler>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<PullMessage>(), It.IsAny<PullResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SendAsync(), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }
        }

        public class CommitTransactionAsync
        {
            [Fact]
            public async Task ShouldDelegateToV3()
            {
                var mockV3 = new Mock<IBoltProtocol>();
                var mockConn = new Mock<IConnection>();
                var mockBt = new Mock<IBookmarksTracker>();
                var protocol = new BoltProtocol(mockV3.Object);

                await protocol.CommitTransactionAsync(mockConn.Object, mockBt.Object);

                mockV3.Verify(x => x.CommitTransactionAsync(mockConn.Object, mockBt.Object), Times.Once);
                mockConn.Verify(x => x.CommitTransactionAsync(It.IsAny<IBookmarksTracker>()), Times.Never());
            }
        }

        public class RollbackTransactionAsync
        {
            [Fact]
            public async Task ShouldDelegateToV3()
            {
                var mockV3 = new Mock<IBoltProtocol>();
                var mockConn = new Mock<IConnection>();
                var protocol = new BoltProtocol(mockV3.Object);

                await protocol.RollbackTransactionAsync(mockConn.Object);

                mockV3.Verify(x => x.RollbackTransactionAsync(mockConn.Object), Times.Once);
                mockConn.Verify(x => x.RollbackTransactionAsync(), Times.Never());
            }
        }

        public class LoginAsync
        {
            [Fact]
            public async Task ShouldDelegateToV3()
            {
                var mockV3 = new Mock<IBoltProtocol>();
                var mockConn = new Mock<IConnection>();
                var protocol = new BoltProtocol(mockV3.Object);
                var auth = AuthTokens.Basic("x", "y");
                var ua = "herman";

                await protocol.LoginAsync(mockConn.Object, ua, auth);

                mockV3.Verify(x => x.LoginAsync(mockConn.Object, ua, auth), Times.Once);
                mockConn.Verify(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<IAuthToken>()), Times.Never());
            }
        }

        public class LogoutAsync
        {
            [Fact]
            public async Task ShouldDelegateToV3()
            {
                var mockV3 = new Mock<IBoltProtocol>();
                var mockConn = new Mock<IConnection>();
                var protocol = new BoltProtocol(mockV3.Object);

                await protocol.LogoutAsync(mockConn.Object);

                mockV3.Verify(x => x.LogoutAsync(mockConn.Object), Times.Once);
                mockConn.Verify(x => x.LogoutAsync(), Times.Never());
            }
        }

        public class ResetAsync
        {
            [Fact]
            public async Task ShouldDelegateToV3()
            {
                var mockV3 = new Mock<IBoltProtocol>();
                var mockConn = new Mock<IConnection>();
                var protocol = new BoltProtocol(mockV3.Object);

                await protocol.ResetAsync(mockConn.Object);

                mockV3.Verify(x => x.ResetAsync(mockConn.Object), Times.Once);
                mockConn.Verify(x => x.ResetAsync(), Times.Never());
            }
        }

        public class GetRoutingTableAsync
        {
            [Fact]
            public async Task ShouldThrowAnExceptionWhneNullConnection()
            {
                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.GetRoutingTableAsync(null, null, "douglas fir", null));

                exception.Should().BeOfType<ProtocolException>();
            }

            [Theory]
            [InlineData(4, 3)]
            [InlineData(4, 2)]
            [InlineData(4, 1)]
            [InlineData(4, 0)]
            public async Task ShouldThrowWhenUsingImpersonatedUserWithBoltVersionLessThan44(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.GetRoutingTableAsync(mockConn.Object, null, "douglas fir", null));

                exception.Should().BeOfType<ArgumentException>();
            }

            [Theory]
            [InlineData(4, 4)]
            [InlineData(5, 0)]
            [InlineData(6, 0)]
            public async Task ShouldNotThrowWhenImpersonatingUserWithBoltVersionGreaterThan43(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocol.Instance.GetRoutingTableAsync(mockConn.Object, null, "douglas fir", null));

                exception.Should().BeNull();
            }
        }
    }
}
