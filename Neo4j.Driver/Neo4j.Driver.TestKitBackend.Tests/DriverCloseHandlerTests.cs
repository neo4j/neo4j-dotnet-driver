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

public class DriverCloseHandlerTests
{
    [Fact]
    public async Task Closes_the_driver_removes_it_from_the_registry_and_responds_with_its_id()
    {
        var driverMock = new Mock<IDriver>();
        var registered = new RegistryObject<IDriver>("driver-1", driverMock.Object);

        var registry = new Mock<IRegistry>();
        registry.Setup(r => r.Get<IDriver>("driver-1")).Returns(registered);

        var handler = new DriverCloseHandler(registry.Object);
        var request = new DriverCloseRequest { Driver = registered };

        var response = await handler.ProcessAsync(request);

        driverMock.Verify(d => d.DisposeAsync(), Times.Once);
        registry.Verify(r => r.Remove("driver-1"), Times.Once);
        response.Should().BeOfType<DriverResponse>().Subject.Id.Should().Be(registered.Id);
    }
}
