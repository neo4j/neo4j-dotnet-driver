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

using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal;

public class RootContainerFactoryTests
{
    private static DriverContext Context(INeo4jLogger logger = null)
    {
        var builder = Config.Builder;
        if (logger is not null)
        {
            builder = builder.WithLogger(logger);
        }

        return new DriverContext(new("bolt://localhost"), null, builder.Build());
    }

    private static DriverContext ContextWithProfiles(params IPropertyEncryptionProfile[] profiles)
    {
        var config = Config.Builder.WithPropertyEncryptionProfiles(profiles).Build();
        return new DriverContext(new("bolt://localhost"), null, config);
    }

    private static IPropertyEncryptionProfile EnvelopeProfile(string name)
    {
        return PropertyEncryptionProfile.Envelope(
            name,
            Mock.Of<IKeyEncapsulationService>(),
            Mock.Of<IEncapsulatedKeyRepository>());
    }

    [Fact]
    public void Build_RegistersDriverContextInstance()
    {
        var context = Context();

        var scope = RootContainerFactory.Build(context);

        scope.Resolve<DriverContext>().Should().BeSameAs(context);
    }

    [Fact]
    public void Build_WiresLoggingModule()
    {
        var scope = RootContainerFactory.Build(Context());

        scope.Resolve<ILoggingContextTracker>().Should().NotBeNull();
        scope.Resolve<ILoggerFactory>().Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvedLoggerFlowsThroughAdapterToUserSuppliedLogger()
    {
        var userLogger = new Mock<INeo4jLogger>();
        userLogger.Setup(x => x.IsDebugEnabled()).Returns(true);
        var scope = RootContainerFactory.Build(Context(userLogger.Object));

        var logger = (ILogger)scope.Resolve(typeof(ILogger), typeof(RootContainerFactoryTests));
        logger.LogDebug("value is {x}", 42);

        userLogger.Verify(
            x => x.Debug(It.IsAny<string>(), It.Is<object[]>(a => a.Any(o => o as int? == 42))));
    }

    [Fact]
    public void Build_RegistersDateTimeProviderInstance()
    {
        var scope = RootContainerFactory.Build(Context());

        scope.Resolve<IDateTimeProvider>().Should().BeSameAs(DateTimeProvider.Instance);
    }

    [Fact]
    public void Build_ResolvesPropertyEncryption()
    {
        var scope = RootContainerFactory.Build(Context());

        scope.Resolve<IPropertyEncryption>().Should().NotBeNull();
    }

    [Fact]
    public void Build_RegistersConfiguredEncryptionProfilesIntoTheRegistry()
    {
        var profile = EnvelopeProfile("p");

        var scope = RootContainerFactory.Build(ContextWithProfiles(profile));

        scope.Resolve<IEncryptionProfileRegistry>().Get("p").Should().BeSameAs(profile);
    }

    [Fact]
    public void Build_RegistersEveryConfiguredEncryptionProfile()
    {
        var a = EnvelopeProfile("a");
        var b = EnvelopeProfile("b");

        var scope = RootContainerFactory.Build(ContextWithProfiles(a, b));

        var registry = scope.Resolve<IEncryptionProfileRegistry>();
        registry.Get("a").Should().BeSameAs(a);
        registry.Get("b").Should().BeSameAs(b);
    }
}
