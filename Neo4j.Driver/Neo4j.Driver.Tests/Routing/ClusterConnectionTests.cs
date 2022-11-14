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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing;

public class ClusterConnectionTests
{
    private static Uri Uri => new("https://neo4j.com");

    public class OnErrorMethod
    {
        [Fact]
        public async Task ConvertConnectionErrorToSessionExpired()
        {
            var handlerMock = new Mock<IErrorHandler>();
            var clusterConn =
                new ClusterConnection(CreateConnectionWithMode(AccessMode.Read), Uri, handlerMock.Object);

            var inError = new ServiceUnavailableException("Connection error");
            var outError = await Record.ExceptionAsync(() => clusterConn.OnErrorAsync(inError));
            outError.Should().BeOfType<SessionExpiredException>();
            handlerMock.Verify(x => x.OnConnectionErrorAsync(Uri, null, inError), Times.Once);
            handlerMock.Verify(x => x.OnWriteError(Uri, null), Times.Never);
        }

        [Fact]
        public async Task TreatsDatabaseUnavailableAsConnectionError()
        {
            var handlerMock = new Mock<IErrorHandler>();
            var clusterConn =
                new ClusterConnection(CreateConnectionWithMode(AccessMode.Read), Uri, handlerMock.Object);

            var inError = new TransientException("Neo.TransientError.General.DatabaseUnavailable", "Store copying");
            var outError = await Record.ExceptionAsync(() => clusterConn.OnErrorAsync(inError));
            outError.Should().BeEquivalentTo(inError);

            handlerMock.Verify(x => x.OnConnectionErrorAsync(Uri, null, inError));
        }

        [Theory]
        [InlineData("Neo.ClientError.Cluster.NotALeader")]
        [InlineData("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase")]
        public async Task ConvertReadClusterErrorToClientError(string code)
        {
            var handlerMock = new Mock<IErrorHandler>();
            var clusterConn =
                new ClusterConnection(CreateConnectionWithMode(AccessMode.Read), Uri, handlerMock.Object);

            var inError = ErrorExtensions.ParseServerException(code, null);
            var outError = await Record.ExceptionAsync(() => clusterConn.OnErrorAsync(inError));
            outError.Should().BeOfType<ClientException>();
            handlerMock.Verify(x => x.OnConnectionErrorAsync(Uri, null, inError), Times.Never);
            handlerMock.Verify(x => x.OnWriteError(Uri, null), Times.Never);
        }

        [Theory]
        [InlineData("Neo.ClientError.Cluster.NotALeader")]
        [InlineData("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase")]
        public async Task ConvertWriteClusterErrorToSessionExpiredError(string code)
        {
            var handlerMock = new Mock<IErrorHandler>();
            var clusterConn =
                new ClusterConnection(CreateConnectionWithMode(AccessMode.Write), Uri, handlerMock.Object);

            var inError = ErrorExtensions.ParseServerException(code, null);
            var outError = await Record.ExceptionAsync(() => clusterConn.OnErrorAsync(inError));
            outError.Should().BeOfType<SessionExpiredException>();
            handlerMock.Verify(x => x.OnConnectionErrorAsync(Uri, null, inError), Times.Never);
            handlerMock.Verify(x => x.OnWriteError(Uri, null), Times.Once);
        }

        [Fact]
        public async Task ConvertClusterErrorToClientError()
        {
            var handlerMock = new Mock<IErrorHandler>();
            var clusterConn =
                new ClusterConnection(CreateConnectionWithMode(AccessMode.Read), Uri, handlerMock.Object);

            var inError = new ClientException("random error");
            var outError = await Record.ExceptionAsync(() => clusterConn.OnErrorAsync(inError));
            outError.Should().Be(inError);
            handlerMock.Verify(x => x.OnConnectionErrorAsync(Uri, null, inError), Times.Never);
            handlerMock.Verify(x => x.OnWriteError(Uri, null), Times.Never);
        }

        private static IConnection CreateConnectionWithMode(AccessMode mode)
        {
            var connMock = new Mock<IConnection>();
            connMock.Setup(c => c.Mode).Returns(mode);
            return connMock.Object;
        }
    }
}
