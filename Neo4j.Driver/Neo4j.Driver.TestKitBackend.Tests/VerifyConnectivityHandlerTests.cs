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
using Moq;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class VerifyConnectivityHandlerTests
{
    [Fact]
    public async Task Verifies_connectivity_on_the_registered_driver_and_responds_with_its_id()
    {
        var registry = new Registry();
        var driverMock = new Mock<IDriver>();
        var registered = registry.Register(driverMock.Object);
        var handler = new VerifyConnectivityHandler();
        var request = new VerifyConnectivityRequest { Driver = registry.Get<IDriver>(registered.Id) };

        var response = await handler.ProcessAsync(request);

        driverMock.Verify(d => d.VerifyConnectivityAsync(), Times.Once);
        response.Should().BeOfType<DriverResponse>().Subject.Id.Should().Be(registered.Id);
    }
}
