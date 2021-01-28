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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class PooledConnectionTests
    {
        public class HasUnrecoverableError
        {

            [Fact]
            public void ShouldReportErrorIfIsTransientException()
            {
                
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new PooledConnection(SocketConnectionTests.NewSocketConnection(handler:mockResponseHandler.Object));

                mockResponseHandler.Setup(x => x.Error).Returns(new TransientException("BLAH", "lalala"));
                con.HasUnrecoverableError.Should().BeFalse();
            }

            [Fact]
            public void ShouldReportErrorIfIsDatabaseException()
            {
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new PooledConnection(SocketConnectionTests.NewSocketConnection(handler: mockResponseHandler.Object));

                mockResponseHandler.Setup(x => x.HasError).Returns(true);
                mockResponseHandler.Setup(x => x.Error).Returns(new DatabaseException("BLAH", "lalala"));

                var exception = Record.Exception(() => con.ReceiveOne());
                exception.Should().BeOfType<DatabaseException>();
                exception.Message.Should().Be("lalala");

                con.HasUnrecoverableError.Should().BeTrue();
                mockResponseHandler.VerifySet(x => x.Error = null, Times.Once);
            }

            [Fact]
            public void ShouldNotReportErrorIfIsOtherExceptions()
            {
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                var con = new PooledConnection(SocketConnectionTests.NewSocketConnection(handler: mockResponseHandler.Object));

                mockResponseHandler.Setup(x => x.Error).Returns(new ClientException("BLAH", "lalala"));
                con.HasUnrecoverableError.Should().BeFalse();
            }
        }

        public class IsOpenMethod
        {
            [Fact]
            public void ShouldBeFalseWhenConnectionIsNotOpen()
            {
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.IsOpen).Returns(false);
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.Error).Returns(new ClientException()); // has no unrecoverable error

                var conn = new PooledConnection(SocketConnectionTests.NewSocketConnection(mockClient.Object, mockResponseHandler.Object));
                conn.IsOpen.Should().BeFalse();
            }

            [Fact]
            public void ShouldBeFalseWhenConnectionHasUnrecoverableError()
            {
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.IsOpen).Returns(false);
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.Error).Returns(new DatabaseException());  // unrecoverable error

                var conn = new PooledConnection(SocketConnectionTests.NewSocketConnection(mockClient.Object, mockResponseHandler.Object));
                conn.IsOpen.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueWhenIsHealthy()
            {
                var mockClient = new Mock<ISocketClient>();
                mockClient.Setup(x => x.IsOpen).Returns(true);
                var mockResponseHandler = new Mock<IMessageResponseHandler>();
                mockResponseHandler.Setup(x => x.Error).Returns(new ClientException());  // has no unrecoverable error

                var conn = new PooledConnection(SocketConnectionTests.NewSocketConnection(mockClient.Object, mockResponseHandler.Object));
                conn.IsOpen.Should().BeTrue();
            }
        }
    }
}
