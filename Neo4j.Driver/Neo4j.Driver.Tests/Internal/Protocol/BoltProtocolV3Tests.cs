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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Types;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.Protocol
{
    public class BoltProtocolV3Tests
    {
        public class AuthenticateAsyncTests
        {
            [Fact]
            public async Task ShouldSyncHelloMessage()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V5_1);

                var auth = AuthTokens.Basic("user", "pass");

                var mockMsgFactory = new Mock<IBoltProtocolMessageFactory>();
                var msg = new HelloMessage(BoltProtocolVersion.V3_0, "ua", null, null as IDictionary<string, string>);
                mockMsgFactory.Setup(x => x.NewHelloMessage(mockConn.Object, "ua", auth)).Returns(msg);

                var mockHandlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var handler = new HelloResponseHandler(mockConn.Object);
                mockHandlerFactory.Setup(x => x.NewHelloResponseHandler(mockConn.Object)).Returns(handler);

                var protocol = new BoltProtocolV3(mockMsgFactory.Object, mockHandlerFactory.Object);
                await protocol.AuthenticateAsync(mockConn.Object, "ua", auth, null);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<HelloMessage>(), It.IsNotNull<HelloResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.VerifyNoOtherCalls();
            }

            [Theory]
            [InlineData(3, 0)]
            [InlineData(4, 1)]
            [InlineData(5, 1)]
            public async Task ShouldThrowIfNotificationsConfigNonNullPre52(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                var auth = AuthTokens.Basic("user", "pass");
                var mockMsgFactory = new Mock<IBoltProtocolMessageFactory>();
                var mockHandlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var protocol = new BoltProtocolV3(mockMsgFactory.Object, mockHandlerFactory.Object);
                var ex = await Record.ExceptionAsync(
                    () => protocol.AuthenticateAsync(mockConn.Object, "ua", auth, new NotificationsDisabledConfig()));

                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }
        }

        public class LogoutAsyncTests
        {
            [Fact]
            public async Task ShouldSendGoodbyeMessage()
            {
                var mockConn = new Mock<IConnection>();

                await BoltProtocolV3.Instance.LogoutAsync(mockConn.Object);

                mockConn.Verify(x => x.EnqueueAsync(GoodbyeMessage.Instance, NoOpResponseHandler.Instance), Times.Once);
                mockConn.Verify(x => x.SendAsync());
                mockConn.VerifyNoOtherCalls();
            }
        }

        public class ResetAsyncTests
        {
            [Fact]
            public async Task ShouldEnqueueResetMessage()
            {
                var mockConn = new Mock<IConnection>();

                await BoltProtocolV3.Instance.ResetAsync(mockConn.Object);

                mockConn.Verify(x => x.EnqueueAsync(ResetMessage.Instance, NoOpResponseHandler.Instance), Times.Once);
                mockConn.VerifyNoOtherCalls();
            }
        }

        public class GetRoutingTableAsyncTests
        {
            [Fact]
            public async Task ShouldThrowProtocolExceptionIfNullConnection()
            {
                var ex = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.GetRoutingTableAsync(null, "db", null, null));

                ex.Should().BeOfType<ProtocolException>();
            }

            [Fact]
            public async Task ShouldThrowIfImpersonationNotNull()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);
                mockConn.SetupProperty(x => x.SessionConfig);

                var ex = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.GetRoutingTableAsync(mockConn.Object, "db", new("Douglas Fir"), null));

                ex.Should().BeOfType<ArgumentException>();
            }

            [Fact]
            public async Task ShouldSendRunWithMetadataMessageToGetRoutingTable()
            {
                var mockRoutingContext = new Mock<IDictionary<string, string>>();
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);
                mockConn.SetupGet(x => x.RoutingContext).Returns(mockRoutingContext.Object);

                var mockRtResult = new Mock<IRecord>();
                mockRtResult.SetupGet(x => x.Values).Returns(new Dictionary<string, object>());

                var mockCursor = new Mock<IInternalResultCursor>();
                mockCursor.SetupSequence(x => x.FetchAsync()).ReturnsAsync(true).ReturnsAsync(false);
                mockCursor.SetupGet(x => x.Current).Returns(mockRtResult.Object);

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                resultCursorBuilderMock.Setup(x => x.CreateCursor()).Returns(mockCursor.Object);

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();

                AutoCommitParams queryParams = null;
                msgFactory.Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, It.IsAny<AutoCommitParams>(), It.IsAny<INotificationsConfig>()))
                    .Callback<IConnection, AutoCommitParams, INotificationsConfig>((_, y, _) => queryParams = y);

                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
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

                var protocol = new BoltProtocolV3(msgFactory.Object, handlerFactory.Object);

                var bm = new InternalBookmarks();
                var routingTable = await protocol.GetRoutingTableAsync(
                    mockConn.Object,
                    "test",
                    null,
                    bm);

                routingTable.Should().Contain(new KeyValuePair<string, object>("db", "test"));

                handlerFactory.Verify(x => x.NewRouteResponseHandler(), Times.Never);

                msgFactory.Verify(
                    x =>
                        x.NewRouteMessage(
                            It.IsAny<IConnection>(),
                            It.IsAny<Bookmarks>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()),
                    Times.Never);

                queryParams.Should().NotBeNull();
                queryParams.Query.Text.Should().Be("CALL dbms.cluster.routing.getRoutingTable($context)");
                queryParams.Query.Parameters.Should().HaveCount(1).And.ContainKeys("context");
                queryParams.Query.Parameters["context"].Should().Be(mockRoutingContext.Object);
                queryParams.Database.Should().BeNull();
                queryParams.BookmarksTracker.Should().NotBeNull();
                queryParams.ResultResourceHandler.Should().NotBeNull();
                queryParams.Bookmarks.Should().BeNull();

                mockConn.Verify(x => x.ConfigureMode(AccessMode.Read), Times.Once);
                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()),
                    Times.Exactly(2));

                mockConn.Verify(x => x.SendAsync(), Times.Once);
            }
        }

        public class RunInAutoCommitTransactionAsyncTests
        {
            [Fact]
            public async Task ShouldThrowIfImpersonatedUserIsNotNull()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);
                mockConn.SetupProperty(x => x.SessionConfig);

                var acp = new AutoCommitParams
                {
                    SessionConfig = new SessionConfig("Douglas Fir")
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null));

                exception.Should().BeOfType<ArgumentException>();
            }

            [Theory]
            [InlineData(3, 0)]
            [InlineData(4, 0)]
            [InlineData(5, 1)]
            public async Task ShouldThrowWhenNotificationsWithBoltVersionLessThan52(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var acp = new AutoCommitParams
                {
                    Query = new Query("..."),
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.RunInAutoCommitTransactionAsync(
                        mockConn.Object,
                        acp,
                        new NotificationsDisabledConfig()));

                exception.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Theory]
            [InlineData(5, 2)]
            [InlineData(6, 0)]
            public async Task ShouldNotThrowWhenNotificationsWithBoltVersionGreaterThan51(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var acp = new AutoCommitParams
                {
                    Query = new Query("..."),
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.RunInAutoCommitTransactionAsync(
                        mockConn.Object,
                        acp,
                        new NotificationsDisabledConfig()));

                exception.Should().BeNull();
            }

            [Fact]
            public async Task ShouldThrowIfDatabaseIsNotNull()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);

                var acp = new AutoCommitParams
                {
                    Database = "NotNull"
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null));

                exception.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async Task ShouldSendMessages()
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
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory
                    .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, acp, null))
                    .Returns(new RunWithMetadataMessage(mockConn.Object.Version, new Query("...")));

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var summaryBuilder = new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")));
                handlerFactory.Setup(
                        x => x.NewRunResponseHandlerV3(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(new RunResponseHandlerV3(resultCursorBuilderMock.Object, summaryBuilder));

                handlerFactory.Setup(
                        x => x.NewPullAllResponseHandler(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>(),
                            mockBt.Object))
                    .Returns(new PullAllResponseHandler(resultCursorBuilderMock.Object, summaryBuilder, mockBt.Object));

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

                var protocol = new BoltProtocolV3(msgFactory.Object, handlerFactory.Object);
                await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null);

                handlerFactory.Verify(
                    x => x.NewResultCursorBuilder(
                        It.IsNotNull<SummaryBuilder>(),
                        mockConn.Object,
                        null,
                        null,
                        null,
                        mockRrh.Object,
                        Config.Infinite,
                        false),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<IRequestMessage>(), It.IsNotNull<IResponseHandler>()),
                    Times.Exactly(2));

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<RunWithMetadataMessage>(), It.IsNotNull<RunResponseHandlerV3>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(PullAllMessage.Instance, It.IsNotNull<PullAllResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SendAsync(), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }
        }

        public class BeginTransactionAsyncTests
        {
            [Fact]
            public async Task ShouldThrowIfImpersonatedUserIsNotNull()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);
                mockConn.SetupGet(x => x.SessionConfig).Returns(new SessionConfig("Douglas Fire"));

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        new BeginProtocolParams(
                        null,
                        null,
                        TransactionConfig.Default,
                        null,
                        null,
                        true)));

                exception.Should().BeOfType<ArgumentException>();
            }

            [Theory]
            [InlineData(3, 0)]
            [InlineData(4, 0)]
            [InlineData(5, 1)]
            public async Task ShouldThrowWhenNotificationsWithBoltVersionLessThan52(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        new BeginProtocolParams(
                        null,
                        null,
                        TransactionConfig.Default,
                        null,
                        new NotificationsDisabledConfig(),
                        true)));

                exception.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Theory]
            [InlineData(5, 2)]
            [InlineData(6, 0)]
            public async Task ShouldNotThrowWhenNotificationsWithBoltVersionGreaterThan51(int major, int minor)
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        new BeginProtocolParams(
                        null,
                        null,
                        TransactionConfig.Default,
                        null,
                        new NotificationsDisabledConfig(),
                        true)));

                exception.Should().BeNull();
            }

            [Fact]
            public async Task ShouldThrowIfDatabaseIsNotNull()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        new BeginProtocolParams(
                        "db",
                        null,
                        TransactionConfig.Default,
                        null,
                        null,
                        true)));

                exception.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async Task ShouldThrowIfConnectionModeIsNull()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);
                mockConn.SetupGet(x => x.Mode).Returns((AccessMode?)null);

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.BeginTransactionAsync(
                        mockConn.Object,
                        new BeginProtocolParams(
                        null,
                        null,
                        TransactionConfig.Default,
                        null,
                        null,
                        true)));

                exception.Should().BeOfType<InvalidOperationException>();
            }

            [Fact]
            public async Task ShouldSyncBeginMessage()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);
                mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Write);

                var fakeMessage = new BeginMessage(null, null, null, null, AccessMode.Write, null, null);
                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory.Setup(
                        x => x.NewBeginMessage(
                            It.IsAny<IConnection>(),
                            It.IsAny<string>(),
                            It.IsAny<Bookmarks>(),
                            It.IsAny<TransactionConfig>(),
                            It.IsAny<AccessMode>(),
                            It.IsAny<INotificationsConfig>()))
                    .Returns(fakeMessage);

                var protocol = new BoltProtocolV3(msgFactory.Object);

                var tc = new TransactionConfig();
                var bookmarks = new InternalBookmarks();
                await protocol.BeginTransactionAsync(
                    mockConn.Object,
                    new BeginProtocolParams(
                    null,
                    bookmarks,
                    tc,
                    null,
                    null,
                    true));

                msgFactory.Verify(
                    x => x.NewBeginMessage(mockConn.Object, null, bookmarks, tc, AccessMode.Write, null),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(fakeMessage, NoOpResponseHandler.Instance),
                    Times.Once);

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.SendAsync(), Times.Never);
            }
        }

        public class RunInExplicitTransactionAsync
        {
            [Fact]
            public async Task ShouldSendMessages()
            {
                var query = new Query("...");
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V3_0);

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                msgFactory
                    .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, query, null))
                    .Returns(new RunWithMetadataMessage(mockConn.Object.Version, query));

                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var summaryBuilder = new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")));
                handlerFactory.Setup(
                        x => x.NewRunResponseHandlerV3(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>()))
                    .Returns(new RunResponseHandlerV3(resultCursorBuilderMock.Object, summaryBuilder));

                handlerFactory.Setup(
                        x => x.NewPullAllResponseHandler(
                            resultCursorBuilderMock.Object,
                            It.IsNotNull<SummaryBuilder>(),
                            null))
                    .Returns(new PullAllResponseHandler(resultCursorBuilderMock.Object, summaryBuilder, null));

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

                var protocol = new BoltProtocolV3(msgFactory.Object, handlerFactory.Object);
                await protocol.RunInExplicitTransactionAsync(mockConn.Object, query, false);

                handlerFactory.Verify(
                    x => x.NewResultCursorBuilder(
                        It.IsNotNull<SummaryBuilder>(),
                        mockConn.Object,
                        null,
                        null,
                        null,
                        null,
                        Config.Infinite,
                        false),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<IRequestMessage>(), It.IsNotNull<IResponseHandler>()),
                    Times.Exactly(2));

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<RunWithMetadataMessage>(), It.IsNotNull<RunResponseHandlerV3>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(PullAllMessage.Instance, It.IsNotNull<PullAllResponseHandler>()),
                    Times.Once);

                mockConn.Verify(x => x.SendAsync(), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }
        }

        public class CommitTransactionAsyncTests
        {
            [Fact]
            public async Task ShouldSyncCommitMessage()
            {
                var mockConn = new Mock<IConnection>();
                var mockBt = new Mock<IBookmarksTracker>();
                var mockHandler = new Mock<IBoltProtocolHandlerFactory>();
                var handler = new CommitResponseHandler(mockBt.Object);
                mockHandler.Setup(x => x.NewCommitResponseHandler(mockBt.Object))
                    .Returns(handler);

                var protocol = new BoltProtocolV3(null, mockHandler.Object);

                await protocol.CommitTransactionAsync(mockConn.Object, mockBt.Object);

                mockConn.Verify(x => x.EnqueueAsync(CommitMessage.Instance, handler), Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.VerifyNoOtherCalls();
            }
        }

        public class RollbackTransactionAsyncTests
        {
            [Fact]
            public async Task ShouldSyncRollbackMessage()
            {
                var mockConn = new Mock<IConnection>();

                await BoltProtocolV3.Instance.RollbackTransactionAsync(mockConn.Object);

                mockConn.Verify(
                    x => x.EnqueueAsync(RollbackMessage.Instance, NoOpResponseHandler.Instance),
                    Times.Once);

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.VerifyNoOtherCalls();
            }
        }
    }
}
