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
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

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
                var msg = new HelloMessage(BoltProtocolVersion.V30, "ua", null, null);
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
