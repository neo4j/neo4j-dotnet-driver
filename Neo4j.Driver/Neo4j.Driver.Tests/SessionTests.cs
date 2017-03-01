// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SessionTests
    {
        internal static Session NewSession(IConnection connection, ILogger logger=null, AccessMode mode = AccessMode.Write, string bookmark = null)
        {
            return new Session(new TestConnectionProvider(connection), logger, mode, bookmark);
        }

        public class RunMethod
        {
            [Fact]
            public void ShouldSendOnRun()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                session.Run("lalalal");

                mockConn.Verify(x => x.Run("lalalal", null, It.IsAny<ResultBuilder>(), true), Times.Once);
                mockConn.Verify(x => x.Send());
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                session.Run("lalalal");

                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void ShouldIgnoreBookmark()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object, bookmark:"a bookmark");
                session.LastBookmark.Should().Be("a bookmark");
                session.BeginTransaction("set new bookmark");
                session.LastBookmark.Should().Be("set new bookmark");
            }

            [Fact]
            public void ShouldNotAllowNewTxWhileOneIsRunning()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                session.BeginTransaction();
                var error = Record.Exception(() => session.BeginTransaction());
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToOpenTxAfterPreviousIsClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();
                tx = session.BeginTransaction();
            }

            [Fact]
            public void ShouldNotBeAbleToUseSessionWhileOngoingTransaction()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();

                var error = Record.Exception(() => session.Run("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToUseSessionAgainWhenTransactionIsClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();

                session.Run("lalal");
            }

            [Fact]
            public void ShouldClosePreviousRunConnectionWhenRunMoreStatements()
            {
                var mockConn = new Mock<IConnection>();
                var session = NewSession(mockConn.Object);
                session.Run("lalal");

                session.Run("bibib");
                mockConn.Verify(c=>c.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldClosePreviousRunConnectionWhenRunMoreTransactions()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(false);
                var session = NewSession(mockConn.Object);
                session.Run("lala");

                session.BeginTransaction();
                mockConn.Verify(c=>c.Dispose(), Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldDisposeTxOnDispose()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();
                session.Dispose();

                mockConn.Verify(x => x.Run("ROLLBACK", null, null, false), Times.Once);
            }

            [Fact]
            public void ShouldDisposeConnectinOnDispose()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                session.Run("lalal");
                session.Dispose();

                mockConn.Verify(x => x.Sync(), Times.Once);
                mockConn.Verify(x => x.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionWhenDisposingSessionMoreThanOnce()
            {
                // Given
                var mockConn = new Mock<IConnection>();
                var session = NewSession(mockConn.Object);

                // When
                session.Dispose();
                var exception = Record.Exception(()=>session.Dispose());

                // Then
                exception.Should().BeOfType<ObjectDisposedException>();
                exception.Message.Should().Contain("Failed to dispose this seesion as it has already been disposed.");
            }
        }

        private class TestConnectionProvider : IConnectionProvider
        {
            private IConnection Connection { get; set; }
            private AccessMode Mode { get; set; }

            public TestConnectionProvider(IConnection connection)
            {
                Connection = connection;
            }
            public void Dispose()
            {
                // do nothing
            }

            public IConnection Acquire(AccessMode mode)
            {
                Mode = mode;
                return Connection;
            }
        }
    }
}