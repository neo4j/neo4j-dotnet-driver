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
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal;

public class RootContainerFactoryTests
{
    private static DriverContext Context(INeo4jLogger logger = null)
    {
        var config = logger is null ? new Config() : Config.Builder.WithLogger(logger).Build();
        return new DriverContext(new("bolt://localhost"), null, config);
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
            x => x.Debug("[RootContainerFactoryTests] value is {0}", It.Is<object[]>(a => a[0].Equals(42))));
    }
}
