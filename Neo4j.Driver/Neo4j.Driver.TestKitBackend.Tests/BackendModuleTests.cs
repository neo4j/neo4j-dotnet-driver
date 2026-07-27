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

using System.Text.Json;
using Autofac;
using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class BackendModuleTests
{
    private static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<BackendModule>();
        return builder.Build();
    }

    [Fact]
    public void Json_options_in_a_connection_scope_resolve_handles_registered_through_that_scopes_registry()
    {
        var container = BuildContainer();
        using var scope = container.BeginLifetimeScope();

        var registered = scope.Resolve<IRegistry>().Register(new Stored());
        var options = scope.Resolve<IJsonOptionsProvider>().GetOptions();

        var request = JsonSerializer.Deserialize<Request>(
            $$"""{"thingId":"{{registered.Id}}"}""", options);

        request!.Thing.Object.Should().BeSameAs(registered.Object);
    }

    [Fact]
    public void Handles_registered_in_one_connection_scope_do_not_resolve_in_another()
    {
        var container = BuildContainer();
        using var scopeA = container.BeginLifetimeScope();
        using var scopeB = container.BeginLifetimeScope();

        var registered = scopeA.Resolve<IRegistry>().Register(new Stored());
        var options = scopeB.Resolve<IJsonOptionsProvider>().GetOptions();

        var act = () => JsonSerializer.Deserialize<Request>(
            $$"""{"thingId":"{{registered.Id}}"}""", options);

        act.Should().Throw<TestKitProtocolException>();
    }

    private record Request
    {
        public RegistryObject<Stored> Thing { get; init; } = null!;
    }

    private class Stored;
}
