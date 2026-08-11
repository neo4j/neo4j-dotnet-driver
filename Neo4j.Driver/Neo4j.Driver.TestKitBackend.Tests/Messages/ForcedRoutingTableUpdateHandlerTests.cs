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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ForcedRoutingTableUpdateHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ForcedRoutingTableUpdateHandler>();

    [Fact]
    public async Task Forces_a_routing_table_update_and_responds_with_the_driver_id()
    {
        var driverMock = _autoMocker.GetMock<IInternalDriver>();
        driverMock
            .Setup(d => d.ForceRoutingTableUpdateAsync("adb", It.IsAny<Bookmarks>()))
            .ReturnsAsync(Mock.Of<IRoutingTable>());

        var registered = new Stored<IDriver>("driver-1", driverMock.Object);

        var handler = _autoMocker.CreateInstance<ForcedRoutingTableUpdateHandler>();
        var request = new ForcedRoutingTableUpdateRequest
        {
            Driver = registered,
            Database = "adb",
            Bookmarks = new[] { "bm1" }
        };

        await handler.ProcessAsync(request);

        driverMock.Verify(
            d => d.ForceRoutingTableUpdateAsync("adb", It.Is<Bookmarks>(b => b.Values.Single() == "bm1")),
            Times.Once);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DriverResponse("driver-1")), Times.Once);
    }
}
