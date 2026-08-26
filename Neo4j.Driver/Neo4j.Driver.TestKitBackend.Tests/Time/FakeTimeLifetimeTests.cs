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

using Autofac;
using FluentAssertions;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Time;

[Collection(FakeSystemClockCollection.Name)]
public class FakeTimeLifetimeTests : IDisposable
{
    private readonly IDateTimeProvider _original = DateTimeProvider.StaticInstance;

    public void Dispose()
    {
        DateTimeProvider.StaticInstance = _original;
    }

    private static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<BackendModule>();
        return builder.Build();
    }

    [Fact]
    public void A_clock_left_installed_by_a_connection_is_uninstalled_when_that_connection_ends()
    {
        var container = BuildContainer();

        using (var scope = container.BeginLifetimeScope())
        {
            scope.Resolve<IFakeTimeService>().Install();
            DateTimeProvider.StaticInstance.Should().NotBeSameAs(_original);
        }

        DateTimeProvider.StaticInstance.Should().BeSameAs(_original);
    }

    [Fact]
    public void Each_connection_gets_its_own_fake_time_service()
    {
        var container = BuildContainer();
        using var scopeA = container.BeginLifetimeScope();
        using var scopeB = container.BeginLifetimeScope();

        scopeA.Resolve<IFakeTimeService>().Should().NotBeSameAs(scopeB.Resolve<IFakeTimeService>());
    }
}
