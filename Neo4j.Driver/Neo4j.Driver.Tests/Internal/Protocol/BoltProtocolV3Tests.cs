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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.Protocol
{
    public class BoltProtocolV3Tests
    {
        public class LoginAsyncTests
        {
            [Fact]
            public async Task ShouldSyncHelloMessage()
            {
                var mockConn = new Mock<IConnection>();
                var auth = AuthTokens.Basic("user", "pass");
                
                var mockMsgFactory = new Mock<IBoltProtocolMessageFactory>();
                var msg = new HelloMessage(BoltProtocolVersion.V3_0, "ua", null, null);
                mockMsgFactory.Setup(x => x.NewHelloMessage(mockConn.Object, "ua", auth)).Returns(msg);
                
                var mockHandlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var handler = new HelloResponseHandler(mockConn.Object);
                mockHandlerFactory.Setup(x => x.NewHelloResponseHandler(mockConn.Object)).Returns(handler);
                
                var protocol = new BoltProtocolV3(mockMsgFactory.Object, mockHandlerFactory.Object);
                await protocol.LoginAsync(mockConn.Object, "ua", auth);
                
                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsNotNull<HelloMessage>(), It.IsNotNull<HelloResponseHandler>()),
                    Times.Once);
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.VerifyNoOtherCalls();
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
                
                var ex = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.GetRoutingTableAsync(mockConn.Object, "db", "Douglas Fir", null));

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
                msgFactory.Setup(x => x.NewRunWithMetadataMessage(mockConn.Object, It.IsAny<AutoCommitParams>()))
                    .Callback<IConnection, AutoCommitParams>((_, y) => queryParams = y);
                
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
                var routingTable = await protocol.GetRoutingTableAsync(mockConn.Object,
                    "test",
                    null,
                    bm);

                routingTable.Should().Contain(new KeyValuePair<string, object>("db", "test"));
                
                handlerFactory.Verify(x => x.NewRouteResponseHandler(), Times.Never);
                
                msgFactory.Verify(x => 
                    x.NewRouteMessage(It.IsAny<IConnection>(), 
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

                var acp = new AutoCommitParams
                {
                    ImpersonatedUser = "Douglas Fir"
                };

                var exception = await Record.ExceptionAsync(
                    () => BoltProtocolV3.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp));

                exception.Should().BeOfType<ArgumentException>();
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
                    () => BoltProtocolV3.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp));

                exception.Should().BeOfType<ClientException>();
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

                mockConn.Verify(x => x.EnqueueAsync(RollbackMessage.Instance, NoOpResponseHandler.Instance),
                    Times.Once);

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.VerifyNoOtherCalls();
            }
        }
        
        
    }
}
