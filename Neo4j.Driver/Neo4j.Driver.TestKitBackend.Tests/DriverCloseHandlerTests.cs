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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class DriverCloseHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<DriverCloseHandler>();

    [Fact]
    public async Task Closes_the_driver_removes_it_from_the_registry_and_responds_with_its_id()
    {
        var driverMock = _autoMocker.GetMock<IDriver>();
        var registered = new RegistryObject<IDriver>("driver-1", driverMock.Object);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Get<IDriver>("driver-1")).Returns(registered);

        var handler = _autoMocker.CreateInstance<DriverCloseHandler>();
        var request = new DriverCloseRequest { Driver = registered };

        var response = await handler.ProcessAsync(request);

        driverMock.Verify(d => d.DisposeAsync(), Times.Once);
        _autoMocker.GetMock<IRegistry>().Verify(r => r.Remove("driver-1"), Times.Once);
        response.Should().BeOfType<DriverResponse>().Subject.Id.Should().Be(registered.Id);
    }
}
