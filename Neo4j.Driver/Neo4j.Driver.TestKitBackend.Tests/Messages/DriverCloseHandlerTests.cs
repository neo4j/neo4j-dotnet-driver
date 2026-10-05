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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class DriverCloseHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<DriverCloseHandler>();

    public DriverCloseHandlerTests()
    {
        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Setup(s => s.GetAllRepositories(It.IsAny<IDriver>()))
            .Returns([]);
        _autoMocker.GetMock<IOutboundRoundTrip>()
            .Setup(r => r.SendExpectingAsync<bool>(It.IsAny<IProtocolMessage>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Closes_the_driver_and_responds_with_its_id()
    {
        var driverMock = _autoMocker.GetMock<IDriver>();

        var handler = _autoMocker.CreateInstance<DriverCloseHandler>();
        var request = new DriverCloseRequest { Driver = driverMock.Object, DriverId = "driver-1" };

        await handler.ProcessAsync(request);

        driverMock.Verify(d => d.DisposeAsync(), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DriverResponse("driver-1")), Times.Once);
    }

    [Fact]
    public async Task Tells_the_frontend_to_forget_every_repository_the_driver_had()
    {
        var driverMock = _autoMocker.GetMock<IDriver>();
        var repository1 = Mock.Of<ITestkitEncapsulatedKeyRepository>(r => r.RepositoryId == "repo-1");
        var repository2 = Mock.Of<ITestkitEncapsulatedKeyRepository>(r => r.RepositoryId == "repo-2");
        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Setup(s => s.GetAllRepositories(driverMock.Object))
            .Returns([repository1, repository2]);

        var sentRequests = new List<IProtocolMessage>();
        _autoMocker.GetMock<IOutboundRoundTrip>()
            .Setup(r => r.SendExpectingAsync<bool>(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(sentRequests.Add)
            .ReturnsAsync(true);

        var handler = _autoMocker.CreateInstance<DriverCloseHandler>();
        var request = new DriverCloseRequest { Driver = driverMock.Object, DriverId = "driver-1" };

        await handler.ProcessAsync(request);

        sentRequests.Should()
            .ContainEquivalentOf(new EncapsulatedKeyRepositoryClosed("repo-1"))
            .And.ContainEquivalentOf(new EncapsulatedKeyRepositoryClosed("repo-2"));
    }
}
