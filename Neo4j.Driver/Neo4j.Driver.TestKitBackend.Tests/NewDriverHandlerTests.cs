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

public class NewDriverHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<NewDriverHandler>();

    [Fact]
    public async Task Registers_a_driver_and_responds_with_its_id()
    {
        var registry = new Registry();
        _autoMocker.Use<IRegistry>(registry);
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();
        var request = new NewDriverRequest
        {
            Uri = "bolt://localhost:7687",
            AuthorizationToken = new AuthorizationToken("basic", "neo4j", "secret")
        };

        var response = await handler.ProcessAsync(request);

        var driverResponse = response.Should().BeOfType<DriverResponse>().Subject;
        registry.Get<IDriver>(driverResponse.Id).Object.Should().NotBeNull();
    }

    [Fact]
    public async Task Applies_config_via_the_config_mapper()
    {
        _autoMocker.Use<IRegistry>(new Registry());
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();
        var request = new NewDriverRequest
        {
            Uri = "bolt://localhost:7687",
            AuthorizationToken = new AuthorizationToken("basic", "neo4j", "secret")
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<INewDriverConfigMapper>()
            .Verify(m => m.Apply(request, It.IsAny<IConfigBuilder>()), Times.Once);
    }
}
