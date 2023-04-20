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
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class PooledConnectionTests
    {
        [Theory]
        [InlineData(typeof(Neo4jException))]
        [InlineData(typeof(DatabaseException))]
        [InlineData(typeof(ServiceUnavailableException))]
        [InlineData(typeof(SessionExpiredException))]
        [InlineData(typeof(ProtocolException))]
        [InlineData(typeof(SecurityException))]
        [InlineData(typeof(AuthenticationException))]
        [InlineData(typeof(AuthorizationException))]
        [InlineData(typeof(TokenExpiredException))]
        public async Task ShouldHaveUnrecoverableErrorOnErrorAsync(Type exceptionType)
        {
            var connection = new Mock<IConnection>().Object;
            var releaseManager = new Mock<IConnectionReleaseManager>().Object;
            var pooledConnection = new PooledConnection(connection, releaseManager);
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Testing exception");

            var resultingException = await Record.ExceptionAsync(() => pooledConnection.OnErrorAsync(exception));
            Assert.Equal(resultingException.GetType(), exceptionType);
            Assert.True(pooledConnection.HasUnrecoverableError);
        }

        [Theory]
        [InlineData(typeof(IOException))]
        [InlineData(typeof(SocketException))]
        public async Task ShouldReturnConnectionErrorOnErrorAsync(Type exceptionType)
        {
            var connection = new Mock<IConnection>().Object;
            var releaseManager = new Mock<IConnectionReleaseManager>().Object;
            var pooledConnection = new PooledConnection(connection, releaseManager);
            var exception = (Exception)Activator.CreateInstance(exceptionType);

            var resultingException = await Record.ExceptionAsync(() => pooledConnection.OnErrorAsync(exception));
            Assert.Equal(resultingException.GetType(), typeof(ServiceUnavailableException));
        }

        [Fact]
        public async Task ShouldCloseConnectionOnAuthorizationException()
        {
            var connection = new Mock<IConnection>();
            var releaseManager = new Mock<IConnectionReleaseManager>();
            var pooledConnection = new PooledConnection(
                connection.Object,
                releaseManager.Object);

            var resultException = await Record.ExceptionAsync(
                () =>
                    pooledConnection.OnErrorAsync(new AuthorizationException("Authorization error")));

            releaseManager.Verify(rm => rm.MarkConnectionsForReauthorization(pooledConnection), Times.Once());
        }
    }
}
