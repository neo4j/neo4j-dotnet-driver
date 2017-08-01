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
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class ClusterConnectionTests
    {
        private static Uri Uri => new Uri("https://neo4j.com");
        public class OnErrorMethod
        {
            [Fact]
            public void ConvertConnectionErrorToSessionExpired()
            {
                var connMock = new Mock<IConnection>();
                var handlerMock = new Mock<IClusterErrorHandler>();
                var clusterConn = new ClusterConnection(connMock.Object, Uri, AccessMode.Read, handlerMock.Object);

                var inError = new ServiceUnavailableException("Connection error");
                var outError = Record.Exception(()=>clusterConn.OnError(inError));
                outError.Should().BeOfType<SessionExpiredException>();
                handlerMock.Verify(x=>x.OnConnectionError(Uri, inError), Times.Once);
                handlerMock.Verify(x=>x.OnWriteError(Uri), Times.Never);
            }

            [Fact]
            public void TreatsDatabaseUnavailableAsConnectionError()
            {
                var connMock = new Mock<IConnection>();
                var handlerMock = new Mock<IClusterErrorHandler>();
                var clusterConn = new ClusterConnection(connMock.Object, Uri, AccessMode.Read, handlerMock.Object);

                var inError = new TransientException("Neo.TransientError.General.DatabaseUnavailable", "Store copying");
                var outError = Record.Exception(() => clusterConn.OnError(inError));
                outError.ShouldBeEquivalentTo(inError);

                handlerMock.Verify(x => x.OnConnectionError(Uri, inError));
            }

            [Theory]
            [InlineData("Neo.ClientError.Cluster.NotALeader")]
            [InlineData("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase")]
            public void ConvertReadClusterErrorToClientError(string code)
            {
                var connMock = new Mock<IConnection>();
                var handlerMock = new Mock<IClusterErrorHandler>();
                var clusterConn = new ClusterConnection(connMock.Object, Uri, AccessMode.Read, handlerMock.Object);

                var inError = ErrorExtensions.ParseServerException(code, null);
                var outError = Record.Exception(() => clusterConn.OnError(inError));
                outError.Should().BeOfType<ClientException>();
                handlerMock.Verify(x => x.OnConnectionError(Uri, inError), Times.Never);
                handlerMock.Verify(x => x.OnWriteError(Uri), Times.Never);
            }

            [Theory]
            [InlineData("Neo.ClientError.Cluster.NotALeader")]
            [InlineData("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase")]
            public void ConvertWriteClusterErrorToSessionExpiredError(string code)
            {
                var connMock = new Mock<IConnection>();
                var handlerMock = new Mock<IClusterErrorHandler>();
                var clusterConn = new ClusterConnection(connMock.Object, Uri, AccessMode.Write, handlerMock.Object);

                var inError = ErrorExtensions.ParseServerException(code, null);
                var outError = Record.Exception(() => clusterConn.OnError(inError));
                outError.Should().BeOfType<SessionExpiredException>();
                handlerMock.Verify(x => x.OnConnectionError(Uri, inError), Times.Never);
                handlerMock.Verify(x => x.OnWriteError(Uri), Times.Once);
            }

            [Fact]
            public void ConvertClusterErrorToClientError()
            {
                var connMock = new Mock<IConnection>();
                var handlerMock = new Mock<IClusterErrorHandler>();
                var clusterConn = new ClusterConnection(connMock.Object, Uri, AccessMode.Read, handlerMock.Object);

                var inError = new ClientException("random error");
                var outError = Record.Exception(() => clusterConn.OnError(inError));
                outError.Should().Be(inError);
                handlerMock.Verify(x => x.OnConnectionError(Uri, inError), Times.Never);
                handlerMock.Verify(x => x.OnWriteError(Uri), Times.Never);
            }
        }
    }
}
