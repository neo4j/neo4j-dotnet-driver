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
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class NewDriverHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<NewDriverHandler>();

    private static NewDriverRequest MinimalRequest()
    {
        return new NewDriverRequest
        {
            Uri = "bolt://localhost:7687",
            AuthorizationToken = new AuthorizationToken("basic", "neo4j", "secret")
        };
    }

    private void RegisterCreatedDriverAs(string id)
    {
        _autoMocker.GetMock<IRegistry>()
            .Setup(r => r.Register(It.IsAny<IDriver>()))
            .Returns((IDriver driver) => new RegistryObject<IDriver>(id, driver));
    }

    [Fact]
    public async Task Registers_a_driver_and_responds_with_its_id()
    {
        RegisterCreatedDriverAs("driver-1");
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(MinimalRequest());

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DriverResponse("driver-1")), Times.Once);
    }

    [Fact]
    public async Task Applies_config_via_the_config_mapper()
    {
        RegisterCreatedDriverAs("driver-1");
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();
        var request = MinimalRequest();

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<INewDriverConfigMapper>()
            .Verify(m => m.Apply(request, It.IsAny<IConfigBuilder>()), Times.Once);
    }

    [Fact]
    public async Task Configures_the_driver_with_the_injected_neo4j_logger()
    {
        IDriver? created = null;
        _autoMocker.GetMock<IRegistry>()
            .Setup(r => r.Register(It.IsAny<IDriver>()))
            .Callback((IDriver driver) => created = driver)
            .Returns((IDriver driver) => new RegistryObject<IDriver>("driver-1", driver));

        var neo4JLogger = Mock.Of<INeo4jLogger>();
        _autoMocker.Use(neo4JLogger);
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(MinimalRequest());

        created!.Config.Neo4JLogger.Should().BeSameAs(neo4JLogger);
    }
}
