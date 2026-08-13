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

using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class GetServerInfoHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<GetServerInfoHandler>();

    [Fact]
    public async Task Returns_the_drivers_server_info()
    {
        var serverInfoMock = _autoMocker.GetMock<IServerInfo>();
        serverInfoMock.SetupGet(i => i.Address).Returns("localhost:7687");
        serverInfoMock.SetupGet(i => i.Agent).Returns("Neo4j/5.20.0");
        serverInfoMock.SetupGet(i => i.ProtocolVersion).Returns("5.4");

        var driverMock = _autoMocker.GetMock<IDriver>();
        driverMock.Setup(d => d.GetServerInfoAsync()).ReturnsAsync(serverInfoMock.Object);

        var handler = _autoMocker.CreateInstance<GetServerInfoHandler>();
        var request = new GetServerInfoRequest { Driver = driverMock.Object };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(new ServerInfoResponse("localhost:7687", "Neo4j/5.20.0", "5.4")),
                Times.Once);
    }
}
