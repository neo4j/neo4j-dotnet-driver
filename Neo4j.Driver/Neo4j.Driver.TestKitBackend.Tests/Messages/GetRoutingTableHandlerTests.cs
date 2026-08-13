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

using FluentAssertions;
using FluentAssertions.Equivalency;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class GetRoutingTableHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<GetRoutingTableHandler>();

    private static Mock<IRoutingTable> RoutingTableWithDatabase(string database)
    {
        var routingTable = new Mock<IRoutingTable>();
        routingTable.SetupGet(rt => rt.Database).Returns(database);
        routingTable.SetupGet(rt => rt.ExpireAfterSeconds).Returns(1000);
        routingTable.SetupGet(rt => rt.Routers).Returns(new List<Uri> { new("bolt://router:9000") });
        routingTable.SetupGet(rt => rt.Readers).Returns(new List<Uri>());
        routingTable.SetupGet(rt => rt.Writers).Returns(new List<Uri>());
        return routingTable;
    }

    private async Task<IProtocolMessage?> ProcessAndCaptureResponse(string? requestedDatabase, IRoutingTable routingTable)
    {
        var driverMock = _autoMocker.GetMock<IInternalDriver>();
        driverMock.Setup(d => d.GetRoutingTable(requestedDatabase)).Returns(routingTable);


        IProtocolMessage? captured = null;
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(m => captured = m)
            .Returns(Task.CompletedTask);

        var handler = _autoMocker.CreateInstance<GetRoutingTableHandler>();
        await handler.ProcessAsync(new GetRoutingTableRequest { Driver = driverMock.Object, Database = requestedDatabase });

        return captured;
    }

    [Fact]
    public async Task Reports_the_default_database_as_null_when_the_routing_table_uses_the_empty_string_sentinel()
    {
        var captured = await ProcessAndCaptureResponse(null, RoutingTableWithDatabase(string.Empty).Object);

        captured.Should().BeOfType<RoutingTableResponse>().Which.Should().BeEquivalentTo(
            new RoutingTableResponse(null, 1000, new[] { "router:9000" }, Array.Empty<string>(), Array.Empty<string>()),
            ComparingByMembers);
    }

    [Fact]
    public async Task Reports_a_named_database_unchanged()
    {
        var captured = await ProcessAndCaptureResponse("adb", RoutingTableWithDatabase("adb").Object);

        captured.Should().BeOfType<RoutingTableResponse>().Which.Should().BeEquivalentTo(
            new RoutingTableResponse("adb", 1000, new[] { "router:9000" }, Array.Empty<string>(), Array.Empty<string>()),
            ComparingByMembers);
    }

    private static EquivalencyAssertionOptions<RoutingTableResponse> ComparingByMembers(
        EquivalencyAssertionOptions<RoutingTableResponse> options)
    {
        return options.ComparingByMembers<RoutingTableResponse>();
    }
}
