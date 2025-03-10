// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Telemetry;
using Neo4j.Driver.Internal.Types;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Internal.Protocol;

public class BoltProtocolTests
{
    public class AuthenticateAsyncTests
    {
        [Theory]
        [InlineData(4, 1)]
        [InlineData(5, 0)]
        public async Task ShouldDelegateToV3(int major, int minor)
        {
            var mockV3 = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            var protocol = new BoltProtocol(mockV3.Object);
            var auth = AuthTokens.Basic("x", "y");
            var ua = "herman";

            await protocol.AuthenticateAsync(mockConn.Object, ua, auth, null);

            mockV3.Verify(x => x.AuthenticateAsync(mockConn.Object, ua, auth, null), Times.Once);
            mockConn.Verify(
                x => x.LoginAsync(It.IsAny<string>(), It.IsAny<IAuthToken>(), It.IsAny<INotificationsConfig>()),
                Times.Never());
        }

        [Theory]
        [InlineData(4, 1)]
        [InlineData(5, 1)]
        public async Task ShouldThrowIfNotificationsConfigNonNullPreV52(int major, int minor)
        {
            var mockV3 = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            var protocol = new BoltProtocol(mockV3.Object);
            var auth = AuthTokens.Basic("x", "y");
            var ua = "herman";

            var ex = await Record.ExceptionAsync(
                () => protocol.AuthenticateAsync(mockConn.Object, ua, auth, new NotificationsDisabledConfig()));

            ex.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(5, 1)]
        [InlineData(5, 2)]
        public async Task ShouldNotDelegateToBoltV3(int major, int minor)
        {
            var mockV3 = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            var protocol = new BoltProtocol(mockV3.Object);
            var auth = AuthTokens.Basic("x", "y");
            var ua = "herman";

            await protocol.AuthenticateAsync(mockConn.Object, ua, auth, null);
            mockConn.Verify(
                x => x.LoginAsync(
                    It.IsAny<string>(),
                    It.IsAny<IAuthToken>(),
                    It.IsAny<NotificationsDisabledConfig>()),
                Times.Never());

            mockV3.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(5, 2)]
        public async Task ShouldAcceptNotificationsConfig(int major, int minor)
        {
            var mockV3 = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            var protocol = new BoltProtocol(mockV3.Object);
            var auth = AuthTokens.Basic("x", "y");
            var ua = "herman";

            await protocol.AuthenticateAsync(mockConn.Object, ua, auth, new NotificationsDisabledConfig());
            mockConn.Verify(
                x => x.LoginAsync(
                    It.IsAny<string>(),
                    It.IsAny<IAuthToken>(),
                    It.IsNotNull<NotificationsDisabledConfig>()),
                Times.Never());

            mockV3.VerifyNoOtherCalls();
        }
    }

    public class LogoutAsyncTests
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

    public class ResetAsyncTests
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

    public class GetRoutingTableAsyncTests
    {
        [Fact]
        public async Task ShouldThrowAnExceptionWhenNullConnection()
        {
            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.GetRoutingTableAsync(null, null, new SessionConfig("douglas fir"), null));

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
            mockConn.SetupProperty(x => x.SessionConfig);
            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.GetRoutingTableAsync(
                    mockConn.Object,
                    null,
                    new SessionConfig("douglas fir"),
                    null));

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
                () => BoltProtocol.Instance.GetRoutingTableAsync(
                    mockConn.Object,
                    null,
                    new SessionConfig("douglas fir"),
                    null));

            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(4, 4)]
        [InlineData(5, 0)]
        [InlineData(6, 0)]
        public async Task ShouldSyncRouteMessageWhenUsingBoltVersionGreaterThan43(int major, int minor)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewRouteMessage(mockConn.Object, It.IsNotNull<Bookmarks>(), "db", "dougy"))
                .Returns(new RouteMessage(null, null, null, null));

            msgFactory
                .Setup(x => x.NewRouteMessage(mockConn.Object, It.IsNotNull<Bookmarks>(), "db", "dougy"))
                .Returns(new RouteMessage(null, null, null, null));

            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory.Setup(x => x.NewRouteResponseHandler())
                .Returns(new RouteResponseHandler());

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.GetRoutingTableAsync(
                mockConn.Object,
                "db",
                new SessionConfig("dougy"),
                new InternalBookmarks());

            msgFactory.Verify(
                x => x.NewRouteMessage(
                    mockConn.Object,
                    It.IsNotNull<Bookmarks>(),
                    "db",
                    "dougy"),
                Times.Once);

            handlerFactory.Verify(x => x.NewRouteResponseHandler(), Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsNotNull<IRequestMessage>(), It.IsNotNull<IResponseHandler>()),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsNotNull<RouteMessage>(), It.IsNotNull<RouteResponseHandler>()),
                Times.Once);

            mockConn.Verify(x => x.SendAsync(), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldSyncRouteMessageV43WhenUsingBoltVersion43()
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 3));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewRouteMessageV43(mockConn.Object, It.IsNotNull<Bookmarks>(), "db"))
                .Returns(new RouteMessageV43(null, null, null));

            var rrHandler = new RouteResponseHandler();
            rrHandler.RoutingInformation = new Dictionary<string, object>();
            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory.Setup(x => x.NewRouteResponseHandler())
                .Returns(rrHandler);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.GetRoutingTableAsync(mockConn.Object, "db", null, new InternalBookmarks());

            msgFactory.Verify(
                x => x.NewRouteMessageV43(
                    mockConn.Object,
                    It.IsNotNull<Bookmarks>(),
                    "db"),
                Times.Once);

            handlerFactory.Verify(x => x.NewRouteResponseHandler(), Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsNotNull<IRequestMessage>(), It.IsNotNull<IResponseHandler>()),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsNotNull<RouteMessageV43>(), It.IsNotNull<RouteResponseHandler>()),
                Times.Once);

            mockConn.Verify(x => x.SendAsync(), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldUpdate43RoutingTableResultWithDbKey()
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 3));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewRouteMessageV43(mockConn.Object, It.IsNotNull<Bookmarks>(), "test"))
                .Returns(new RouteMessageV43(null, null, null));

            var mockRt = new Mock<IDictionary<string, object>>();
            mockRt.As<IReadOnlyDictionary<string, object>>();
            var rrHandler = new RouteResponseHandler();
            rrHandler.RoutingInformation = mockRt.Object;
            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory.Setup(x => x.NewRouteResponseHandler())
                .Returns(rrHandler);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.GetRoutingTableAsync(mockConn.Object, "test", null, new InternalBookmarks());

            mockRt.Verify(x => x.Add("db", "test"), Times.Once);

            msgFactory.Verify(
                x => x.NewRouteMessageV43(
                    mockConn.Object,
                    It.IsNotNull<Bookmarks>(),
                    "test"),
                Times.Once);

            handlerFactory.Verify(x => x.NewRouteResponseHandler(), Times.Once);
            handlerFactory.Verify(
                x => x.NewRunResponseHandler(It.IsAny<IResultCursorBuilder>(), It.IsAny<SummaryBuilder>()),
                Times.Never);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsNotNull<IRequestMessage>(), It.IsNotNull<IResponseHandler>()),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsNotNull<RouteMessageV43>(), It.IsNotNull<RouteResponseHandler>()),
                Times.Once);

            mockConn.Verify(x => x.SendAsync(), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Once);
        }

        [Theory]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        public async Task ShouldUseQueryToFetchRoutingTableForBoltVersionLessThan43(int major, int minor)
        {
            var mockRoutingContext = new Mock<IDictionary<string, string>>();
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            mockConn.SetupGet(x => x.RoutingContext).Returns(mockRoutingContext.Object);

            var mockRtResult = new Mock<IRecord>();
            mockRtResult.SetupGet(x => x.Values).Returns(new Dictionary<string, object>());

            var mockCursor = new Mock<IInternalResultCursor>();
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(mockRtResult.Object)
                .Returns(default(IRecord));

            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
            resultCursorBuilderMock.Setup(x => x.CreateCursor()).Returns(mockCursor.Object);

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();

            AutoCommitParams queryParams = null;
            msgFactory.Setup(
                    x => x.NewRunWithMetadataMessage(
                        mockConn.Object,
                        It.IsAny<AutoCommitParams>(),
                        It.IsAny<INotificationsConfig>()))
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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

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
            queryParams.Query.Text.Should().Be("CALL dbms.routing.getRoutingTable($context, $database)");
            queryParams.Query.Parameters.Should().HaveCount(2).And.ContainKeys("context", "database");
            queryParams.Query.Parameters["database"].Should().Be("test");
            queryParams.Query.Parameters["context"].Should().Be(mockRoutingContext.Object);
            queryParams.Database.Should().Be("system");
            queryParams.BookmarksTracker.Should().NotBeNull();
            queryParams.ResultResourceHandler.Should().NotBeNull();
            queryParams.Bookmarks.Should().Be(bm);

            msgFactory.Verify(
                x => x.NewRouteMessageV43(It.IsAny<IConnection>(), It.IsAny<Bookmarks>(), It.IsAny<string>()),
                Times.Never);

            mockConn.Verify(x => x.ConfigureMode(AccessMode.Read), Times.Once);
            mockConn.Verify(
                x => x.EnqueueAsync(
                    It.IsAny<RunWithMetadataMessage>(),
                    It.IsAny<RunResponseHandler>(),
                    It.IsAny<PullMessage>(),
                    It.IsAny<PullResponseHandler>()),
                Times.Exactly(1));

            mockConn.Verify(x => x.SendAsync(), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\t")]
        public async Task ShouldProtectAgainstEmptyDatabaseStringWhenUsingQuery(string dbName)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 1));

            var mockRtResult = new Mock<IRecord>();
            mockRtResult.SetupGet(x => x.Values).Returns(new Dictionary<string, object>());
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator.SetupSequence(x => x.MoveNextAsync()).ReturnsAsync(true).ReturnsAsync(false);
            enumerator.SetupSequence(x => x.Current).Returns(mockRtResult.Object).Returns(default(IRecord));
            var mockCursor = new Mock<IInternalResultCursor>();
            mockCursor.Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(enumerator.Object);

            var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
            resultCursorBuilderMock.Setup(x => x.CreateCursor()).Returns(mockCursor.Object);

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();

            AutoCommitParams queryParams = null;
            msgFactory.Setup(
                    x => x.NewRunWithMetadataMessage(
                        mockConn.Object,
                        It.IsAny<AutoCommitParams>(),
                        It.IsAny<INotificationsConfig>()))
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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.GetRoutingTableAsync(
                mockConn.Object,
                dbName,
                null,
                null);

            queryParams.Should().NotBeNull();
            queryParams.Query.Parameters.Should().HaveCount(2).And.ContainKeys("context", "database");
            queryParams.Query.Parameters["database"].Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("\t")]
        public async Task ShouldProtectAgainstEmptyDatabaseStringWhenUsingRouteMessageV43(string dbName)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 3));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            var rrHandler = new RouteResponseHandler
            {
                RoutingInformation = new Dictionary<string, object>()
            };

            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory.Setup(x => x.NewRouteResponseHandler())
                .Returns(rrHandler);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.GetRoutingTableAsync(mockConn.Object, dbName, null, null);

            msgFactory.Verify(
                x => x.NewRouteMessageV43(
                    mockConn.Object,
                    It.IsAny<Bookmarks>(),
                    null),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\t")]
        public async Task ShouldProtectAgainstEmptyDatabaseStringWhenUsingRouteMessage(string dbName)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 4));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory.Setup(x => x.NewRouteResponseHandler())
                .Returns(new RouteResponseHandler());

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.GetRoutingTableAsync(mockConn.Object, dbName, null, null);

            msgFactory.Verify(
                x => x.NewRouteMessage(
                    mockConn.Object,
                    It.IsAny<Bookmarks>(),
                    null,
                    null),
                Times.Once);
        }
    }

    public class RunInAutoCommitTransactionAsyncTests
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
                SessionConfig = new SessionConfig("Douglas Fir")
            };

            mockConn.SetupGet(x => x.SessionConfig).Returns(new SessionConfig("Douglas Fir"));

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null));

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
                SessionConfig = new SessionConfig("Douglas Fir")
            };

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null));

            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(4, 0)]
        [InlineData(5, 1)]
        public async Task ShouldThrowWhenNotificationsWithBoltVersionLessThan52(int major, int minor)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

            var acp = new AutoCommitParams
            {
                Query = new Query("...")
            };

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(
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
                Query = new Query("...")
            };

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(
                    mockConn.Object,
                    acp,
                    new NotificationsDisabledConfig()));

            exception.Should().BeNull();
        }

        [Fact]
        public async Task ShouldSendPullMessageWhenNotReactive()
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
                .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, acp, null))
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
                        mockConn.Object,
                        mockBt.Object,
                        resultCursorBuilderMock.Object,
                        It.IsNotNull<SummaryBuilder>()))
                .Returns(
                    new PullResponseHandler(
                        resultCursorBuilderMock.Object,
                        new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0"))),
                        mockBt.Object,
                        true));

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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);
            await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null);

            msgFactory.Verify(
                x => x.NewRunWithMetadataMessage(
                    mockConn.Object,
                    acp,
                    null),
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
                    false,
                    It.IsAny<IInternalAsyncTransaction>()),
                Times.Once);

            handlerFactory.Verify(
                x => x.NewRunResponseHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(
                    It.IsNotNull<RunWithMetadataMessage>(),
                    It.IsNotNull<RunResponseHandler>(),
                    It.IsNotNull<PullMessage>(),
                    It.IsNotNull<PullResponseHandler>()),
                Times.Exactly(1));

            mockConn.Verify(x => x.SendAsync(), Times.Once);
            mockConn.Verify(x => x.SyncAsync(), Times.Never);
        }

        [Fact]
        public async Task ShouldSendOnlyRunWhenReactive()
        {
            var mockBt = new Mock<IBookmarksTracker>();
            var mockRrh = new Mock<IResultResourceHandler>();

            var acp = new AutoCommitParams
            {
                Query = new Query("..."),
                Reactive = true,
                FetchSize = 10,
                BookmarksTracker = mockBt.Object,
                ResultResourceHandler = mockRrh.Object,
                TransactionInfo = new TransactionInfo(QueryApiType.AutoCommit, true, false)
            };

            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, acp, null))
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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var mockV3 = new Mock<IBoltProtocol>();
            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);
            await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp, null);

            msgFactory.Verify(
                x => x.NewRunWithMetadataMessage(
                    mockConn.Object,
                    acp,
                    null),
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
                    true,
                    It.IsAny<IInternalAsyncTransaction>()),
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
            mockConn.VerifyGet(x => x.Version);
            mockConn.VerifyGet(x => x.Server);
            mockConn.VerifyGet(x => x.TelemetryEnabled);

            mockConn.VerifyNoOtherCalls();
        }
    }

    public class BeginTransactionAsyncTests
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
            mockConn.SetupProperty(x => x.SessionConfig);

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.BeginTransactionAsync(
                    mockConn.Object,
                    new BeginTransactionParams(
                        "db",
                        Bookmarks.Empty,
                        TransactionConfig.Default,
                        new SessionConfig("Douglas Fir"),
                        null,
                        new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true))));

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
                () => BoltProtocol.Instance.BeginTransactionAsync(
                    mockConn.Object,
                    new BeginTransactionParams(
                        "db",
                        null,
                        TransactionConfig.Default,
                        null,
                        new NotificationsDisabledConfig(),
                        new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true))));

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
                () => BoltProtocol.Instance.BeginTransactionAsync(
                    mockConn.Object,
                    new BeginTransactionParams(
                        "db",
                        null,
                        TransactionConfig.Default,
                        null,
                        new NotificationsDisabledConfig(),
                        new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true))));

            exception.Should().BeNull();
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
                    new BeginTransactionParams(
                        "db",
                        Bookmarks.Empty,
                        TransactionConfig.Default,
                        new SessionConfig("Douglas Fir"),
                        null,
                        new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true))));

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

            var sessionConfig = new SessionConfig("user");

            await protocol.BeginTransactionAsync(
                mockConn.Object,
                new BeginTransactionParams(
                    "db",
                    bookmarks,
                    config,
                    sessionConfig,
                    null,
                    new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true)));

            mockV3.Verify(
                x => x.BeginTransactionAsync(
                    mockConn.Object,
                    new BeginTransactionParams(
                        "db",
                        bookmarks,
                        config,
                        sessionConfig,
                        null,
                        new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true))),
                Times.Once);

            mockConn.Verify(
                x => x.BeginTransactionAsync(It.IsAny<BeginTransactionParams>()),
                Times.Never);
        }
    }

    public class RunInExplicitTransactionAsyncTests
    {
        [Fact]
        public async Task ShouldSendOnlyRunWhenReactive()
        {
            var query = new Query("...");
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

            var mockV3 = new Mock<IBoltProtocol>();

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, query, null))
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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.RunInExplicitTransactionAsync(
                mockConn.Object,
                query,
                true,
                10,
                new Mock<IInternalAsyncTransaction>().Object);

            msgFactory.Verify(
                x => x.NewRunWithMetadataMessage(mockConn.Object, query, null),
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
                    true,
                    It.IsAny<IInternalAsyncTransaction>()),
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
            mockConn.VerifyGet(x => x.Version);
            mockConn.VerifyGet(x => x.Server);
            mockConn.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSendPullMessageWhenNotReactive()
        {
            var query = new Query("...");
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, query, null))
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
                        mockConn.Object,
                        null,
                        resultCursorBuilderMock.Object,
                        It.IsNotNull<SummaryBuilder>()))
                .Returns(
                    new PullResponseHandler(
                        resultCursorBuilderMock.Object,
                        new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0"))),
                        null,
                        true));

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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var protocol = new BoltProtocol(mockV3.Object, msgFactory.Object, handlerFactory.Object);

            await protocol.RunInExplicitTransactionAsync(
                mockConn.Object,
                query,
                false,
                10,
                new Mock<IInternalAsyncTransaction>().Object);

            msgFactory.Verify(
                x => x.NewRunWithMetadataMessage(mockConn.Object, query, null),
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
                    false,
                    It.IsAny<IInternalAsyncTransaction>()),
                Times.Once);

            handlerFactory.Verify(
                x => x.NewRunResponseHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(
                    It.IsAny<RunWithMetadataMessage>(),
                    It.IsAny<RunResponseHandler>(),
                    It.IsAny<PullMessage>(),
                    It.IsAny<PullResponseHandler>()),
                Times.Once);

            mockConn.Verify(x => x.SendAsync(), Times.Once);

            mockConn.VerifyGet(x => x.Version);
            mockConn.VerifyGet(x => x.Server);
            mockConn.VerifyNoOtherCalls();
        }
    }

    public class CommitTransactionAsyncTests
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

    public class RollbackTransactionAsyncTests
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

    public class RequestMoreTests
    {
        [Fact]
        public void ShouldSendPullMessage()
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 4));
            var sb = new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")));

            var mockBt = new Mock<IBookmarksTracker>();
            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewPullMessage(10))
                .Returns(new PullMessage(10));

            var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory
                .Setup(
                    x => x.NewRunResponseHandler(
                        resultCursorBuilderMock.Object,
                        It.IsNotNull<SummaryBuilder>()))
                .Returns(new RunResponseHandler(resultCursorBuilderMock.Object, sb));

            handlerFactory
                .Setup(
                    x => x.NewPullResponseHandler(
                        mockConn.Object,
                        mockBt.Object,
                        resultCursorBuilderMock.Object,
                        It.IsNotNull<SummaryBuilder>()))
                .Returns(new PullResponseHandler(resultCursorBuilderMock.Object, sb, mockBt.Object, true));

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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var protocol = new BoltProtocol(null, msgFactory.Object, handlerFactory.Object);

            protocol.RequestMore(mockConn.Object, sb, mockBt.Object)(resultCursorBuilderMock.Object, 1, 10);

            msgFactory.Verify(x => x.NewPullMessage(1, 10), Times.Once);
            handlerFactory.Verify(
                x => x.NewPullResponseHandler(mockConn.Object, mockBt.Object, resultCursorBuilderMock.Object, sb),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsAny<PullMessage>(), It.IsAny<PullResponseHandler>()),
                Times.Once);

            mockConn.Verify(x => x.SendAsync(), Times.Once);
            mockConn.VerifyNoOtherCalls();
        }
    }

    public class CancelRequestTests
    {
        [Fact]
        public void ShouldSendDiscardMessage()
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(4, 4));
            var sb = new SummaryBuilder(new Query("..."), new ServerInfo(new Uri("http://0.0.0.0")));

            var mockBt = new Mock<IBookmarksTracker>();
            var msgFactory = new Mock<IBoltProtocolMessageFactory>();
            msgFactory
                .Setup(x => x.NewDiscardMessage(1, -1))
                .Returns(new DiscardMessage(1));

            var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();
            var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
            handlerFactory
                .Setup(
                    x => x.NewRunResponseHandler(
                        resultCursorBuilderMock.Object,
                        It.IsNotNull<SummaryBuilder>()))
                .Returns(new RunResponseHandler(resultCursorBuilderMock.Object, sb));

            handlerFactory
                .Setup(
                    x => x.NewPullResponseHandler(
                        mockConn.Object,
                        mockBt.Object,
                        resultCursorBuilderMock.Object,
                        It.IsNotNull<SummaryBuilder>()))
                .Returns(new PullResponseHandler(resultCursorBuilderMock.Object, sb, mockBt.Object, true));

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
                        It.IsAny<bool>(),
                        It.IsAny<IInternalAsyncTransaction>()))
                .Returns(resultCursorBuilderMock.Object);

            var protocol = new BoltProtocol(null, msgFactory.Object, handlerFactory.Object);

            protocol.CancelRequest(mockConn.Object, sb, mockBt.Object)(resultCursorBuilderMock.Object, 1);

            msgFactory.Verify(x => x.NewDiscardMessage(1, -1), Times.Once);
            handlerFactory.Verify(
                x => x.NewPullResponseHandler(mockConn.Object, mockBt.Object, resultCursorBuilderMock.Object, sb),
                Times.Once);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsAny<DiscardMessage>(), It.IsAny<PullResponseHandler>()),
                Times.Once);

            mockConn.Verify(x => x.SendAsync(), Times.Once);
            mockConn.VerifyNoOtherCalls();
        }
    }
}
