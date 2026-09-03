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
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Types;
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
            AuthorizationToken = new AuthorizationToken
            {
                Scheme = "basic",
                Principal = "neo4j",
                Credentials = "secret"
            }
        };
    }

    private void StoreCreatedDriverAs(string id)
    {
        _autoMocker.GetMock<IObjectStore>()
            .Setup(r => r.Store(It.IsAny<IDriver>()))
            .Returns(id);
    }

    [Fact]
    public async Task Stores_a_driver_and_responds_with_its_id()
    {
        StoreCreatedDriverAs("driver-1");
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(MinimalRequest());

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DriverResponse("driver-1")), Times.Once);
    }

    [Fact]
    public async Task Applies_config_via_the_config_mapper()
    {
        StoreCreatedDriverAs("driver-1");
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
        _autoMocker.GetMock<IObjectStore>()
            .Setup(r => r.Store(It.IsAny<IDriver>()))
            .Callback((IDriver driver) => created = driver)
            .Returns((IDriver driver) => "driver-1");

        var neo4JLogger = Mock.Of<INeo4jLogger>();
        _autoMocker.Use(neo4JLogger);
        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(MinimalRequest());

        created!.Config.Neo4JLogger.Should().BeSameAs(neo4JLogger);
    }

    [Fact]
    public async Task Enables_metrics_so_GetConnectionPoolMetrics_can_report_on_the_driver()
    {
        IDriver? created = null;
        _autoMocker.GetMock<IObjectStore>()
            .Setup(r => r.Store(It.IsAny<IDriver>()))
            .Callback((IDriver driver) => created = driver)
            .Returns((IDriver driver) => "driver-1");

        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(MinimalRequest());

        created!.Config.MetricsEnabled.Should().BeTrue();
    }

    private static readonly PropertyEncryptionProfileInput[] RequestedProfiles =
    [
        new("profile-a", new HexBytes([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08])),
        new("profile-b", null)
    ];

    private static NewDriverRequest RequestWithEncryptionProfiles()
    {
        return MinimalRequest() with { PropertyEncryptionProfiles = RequestedProfiles };
    }

    private static IPropertyEncryptionProfile Profile(string name)
    {
        return PropertyEncryptionProfile.Envelope(
            name,
            Mock.Of<IKeyEncapsulationService>(),
            Mock.Of<IEncapsulatedKeyRepository>());
    }

    private DriverEncryptionObjects PrepareReturnsSetupForTheRequestedProfiles()
    {
        var setup = new DriverEncryptionObjects(
            [Profile("profile-a"), Profile("profile-b")],
            new Dictionary<string, ITestkitEncapsulatedKeyRepository>
            {
                ["profile-a"] = Mock.Of<ITestkitEncapsulatedKeyRepository>(),
                ["profile-b"] = Mock.Of<ITestkitEncapsulatedKeyRepository>()
            });

        _autoMocker.GetMock<IDriverEncryptionSetup>()
            .Setup(s => s.Prepare(RequestedProfiles))
            .Returns(setup);

        return setup;
    }

    [Fact]
    public async Task Configures_the_driver_with_the_prepared_encryption_profiles()
    {
        var setup = PrepareReturnsSetupForTheRequestedProfiles();
        IDriver? created = null;
        _autoMocker.GetMock<IObjectStore>()
            .Setup(r => r.Store(It.IsAny<IDriver>()))
            .Callback((IDriver driver) => created = driver)
            .Returns((IDriver driver) => "driver-1");

        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(RequestWithEncryptionProfiles());

        created!.Config.Preview_PropertyEncryptionProfiles.Should().Equal(setup.Profiles);
    }

    [Fact]
    public async Task Stores_the_encryption_objects_against_the_created_driver()
    {
        var setup = PrepareReturnsSetupForTheRequestedProfiles();
        IDriver? created = null;
        _autoMocker.GetMock<IObjectStore>()
            .Setup(r => r.Store(It.IsAny<IDriver>()))
            .Callback((IDriver driver) => created = driver)
            .Returns((IDriver driver) => "driver-1");

        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(RequestWithEncryptionProfiles());

        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Verify(s => s.StoreObjects(created!, setup), Times.Once);
    }

    [Fact]
    public async Task Leaves_the_driver_unencrypted_when_the_request_specifies_no_profiles()
    {
        IDriver? created = null;
        _autoMocker.GetMock<IObjectStore>()
            .Setup(r => r.Store(It.IsAny<IDriver>()))
            .Callback((IDriver driver) => created = driver)
            .Returns((IDriver driver) => "driver-1");

        var handler = _autoMocker.CreateInstance<NewDriverHandler>();

        await handler.ProcessAsync(MinimalRequest());

        created!.Config.Preview_PropertyEncryptionProfiles.Should().BeEmpty();
        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Verify(
                s => s.StoreObjects(It.IsAny<IDriver>(), It.IsAny<DriverEncryptionObjects>()),
                Times.Never);
    }
}
