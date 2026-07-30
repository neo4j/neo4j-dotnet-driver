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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Serialization;
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
    public void The_message_dispatcher_resolves_with_every_handler_constructed()
    {
        var container = BuildContainer();

        // The connection handler registers the transport-bound output into each connection
        // scope, and the host supplies ILoggerFactory (which LoggerMiddleware resolves to
        // satisfy plain ILogger parameters); emulate both so the dispatcher's ResponseWriter
        // dependency can resolve.
        using var scope = container.BeginLifetimeScope(b =>
        {
            b.RegisterInstance(Mock.Of<IConnectionOutput>()).As<IConnectionOutput>();
            b.RegisterInstance(NullLoggerFactory.Instance).As<ILoggerFactory>();
        });

        // Constructs all handlers and builds the type→handler map, so this throws if any
        // handler has an unresolvable dependency or two handlers claim one message type.
        var resolve = () => scope.Resolve<IMessageDispatcher>();

        resolve.Should().NotThrow();
    }

    [Fact]
    public void Classes_injecting_a_plain_ILogger_get_one_categorised_by_their_own_type()
    {
        var container = BuildContainer();
        var factory = new Mock<ILoggerFactory>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);
        using var scope = container.BeginLifetimeScope(b =>
        {
            b.RegisterInstance(Mock.Of<IConnectionOutput>()).As<IConnectionOutput>();
            b.RegisterInstance(factory.Object).As<ILoggerFactory>();
        });

        scope.Resolve<IMessageDispatcher>();

        factory.Verify(f => f.CreateLogger(typeof(NewDriverHandler).FullName!));
        factory.Verify(f => f.CreateLogger(typeof(ResponseWriter).FullName!));
    }

    [Fact]
    public void Singleton_services_resolve_to_one_instance_across_connection_scopes()
    {
        var container = BuildContainer();
        using var scopeA = container.BeginLifetimeScope();
        using var scopeB = container.BeginLifetimeScope();

        scopeA.Resolve<IMessageTypeMap>().Should().BeSameAs(scopeB.Resolve<IMessageTypeMap>());
        scopeA.Resolve<IConnectionIdProvider>().Should().BeSameAs(scopeB.Resolve<IConnectionIdProvider>());
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
