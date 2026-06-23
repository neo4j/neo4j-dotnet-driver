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
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling;

public class HelloResponseHandlerTests
{
    private const string RecvTimeoutKey = "connection.recv_timeout_seconds";

    [Fact]
    public void ShouldApplyServerReadTimeoutWhenNoCapIsConfigured()
    {
        var connection = NewConnection();
        var handler = new HelloResponseHandler(connection.Object);

        handler.OnSuccess(NewSuccessMetadata(60));

        connection.Verify(x => x.SetReadTimeoutInSeconds(60), Times.Once);
    }

    [Fact]
    public void ShouldCapServerReadTimeoutWhenCapIsLower()
    {
        var connection = NewConnection(TimeSpan.FromSeconds(10));
        var handler = new HelloResponseHandler(connection.Object);

        handler.OnSuccess(NewSuccessMetadata(60));

        connection.Verify(x => x.SetReadTimeoutInSeconds(10), Times.Once);
    }

    [Fact]
    public void ShouldKeepServerReadTimeoutWhenCapIsHigher()
    {
        var connection = NewConnection(TimeSpan.FromSeconds(120));
        var handler = new HelloResponseHandler(connection.Object);

        handler.OnSuccess(NewSuccessMetadata(60));

        connection.Verify(x => x.SetReadTimeoutInSeconds(60), Times.Once);
    }

    [Fact]
    public void ShouldRoundFractionalCapUpToWholeSeconds()
    {
        var connection = NewConnection(TimeSpan.FromMilliseconds(1500));
        var handler = new HelloResponseHandler(connection.Object);

        handler.OnSuccess(NewSuccessMetadata(60));

        connection.Verify(x => x.SetReadTimeoutInSeconds(2), Times.Once);
    }

    private static Mock<IConnection> NewConnection(TimeSpan? readTimeoutCap = null)
    {
        var connection = new Mock<IConnection>();
        connection.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V5_8);
        connection.SetupGet(x => x.ConnectionReadTimeoutCap).Returns(readTimeoutCap);
        return connection;
    }

    private static Dictionary<string, object> NewSuccessMetadata(long recvTimeoutSeconds)
    {
        return new Dictionary<string, object>
        {
            [ConfigurationHintsCollector.ConfigHintsKey] = new Dictionary<string, object>
            {
                [RecvTimeoutKey] = recvTimeoutSeconds
            }
        };
    }
}