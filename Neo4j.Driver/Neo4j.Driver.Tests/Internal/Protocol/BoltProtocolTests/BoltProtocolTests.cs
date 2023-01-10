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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.Protocol.BoltProtocolTests
{
    public class BoltProtocolTests
    {
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
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();

                handlerFactory.Setup(
                        x => x.NewResultCursorBuilder(
                            It.IsAny<SummaryBuilder>(),
                            It.IsAny<IConnection>(),
                            It.IsAny<AutoCommitParams>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, long, Task>>>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, Task>>>()))
                    .Returns(resultCursorBuilderMock.Object);

                var protocol = new BoltProtocol(msgFactory.Object, handlerFactory.Object);

                var acp = new AutoCommitParams
                {
                    Query = new Query("...")
                };

                await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp);

                msgFactory.Verify(
                    x => x.NewRunWithMetadataMessage(It.IsNotNull<SummaryBuilder>(), mockConn.Object, acp),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewRunHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()),
                    Times.Once);

                resultCursorBuilderMock.Verify(x => x.CreateCursor(), Times.Once);
            }

            [Fact]
            public async Task ShouldEnqueueTwiceWithReactive()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(5, 0));

                var msgFactory = new Mock<IBoltProtocolMessageFactory>();
                var handlerFactory = new Mock<IBoltProtocolHandlerFactory>();
                var resultCursorBuilderMock = new Mock<IResultCursorBuilder>();

                handlerFactory.Setup(
                        x => x.NewResultCursorBuilder(
                            It.IsAny<SummaryBuilder>(),
                            It.IsAny<IConnection>(),
                            It.IsAny<AutoCommitParams>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, long, Task>>>(),
                            It.IsAny<Func<IConnection, SummaryBuilder, IBookmarksTracker,
                                Func<IResultStreamBuilder, long, Task>>>()))
                    .Returns(resultCursorBuilderMock.Object);

                var protocol = new BoltProtocol(msgFactory.Object, handlerFactory.Object);

                var acp = new AutoCommitParams
                {
                    Query = new Query("..."),
                    Reactive = true
                };

                await protocol.RunInAutoCommitTransactionAsync(mockConn.Object, acp);

                msgFactory.Verify(
                    x => x.NewRunWithMetadataMessage(It.IsNotNull<SummaryBuilder>(), mockConn.Object, acp),
                    Times.Once);

                handlerFactory.Verify(
                    x => x.NewRunHandler(resultCursorBuilderMock.Object, It.IsNotNull<SummaryBuilder>()),
                    Times.Once);

                mockConn.Verify(
                    x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()),
                    Times.Exactly(2));

                resultCursorBuilderMock.Verify(x => x.CreateCursor(), Times.Once);
            }
        }
    }
}
