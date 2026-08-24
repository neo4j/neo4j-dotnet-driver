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
using Autofac.Features.Indexed;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Logging;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class BackendModuleTests
{
    private static IContainer BuildContainer(Action<ContainerBuilder>? configure = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<BackendModule>();
        configure?.Invoke(builder);
        return builder.Build();
    }

    [Fact]
    public void Json_options_resolve_handles_through_the_singleton_objectStore()
    {
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(new TestOutputLoggerFactory()));
        using var scope = container.BeginLifetimeScope();

        var thing = new Thing();
        var id = scope.Resolve<IObjectStore>().Store(thing);
        var options = scope.Resolve<IJsonOptionsProvider>().GetOptions();

        var request = JsonSerializer.Deserialize<Request>($$"""{"thing":"{{id}}"}""", options);

        request!.Thing.Should().BeSameAs(thing);
    }

    [Fact]
    public void Json_options_are_built_once_and_shared_across_connection_scopes()
    {
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(new TestOutputLoggerFactory()));

        using var scopeA = container.BeginLifetimeScope();
        using var scopeB = container.BeginLifetimeScope();

        scopeA.Resolve<IJsonOptionsProvider>().GetOptions()
            .Should().BeSameAs(scopeB.Resolve<IJsonOptionsProvider>().GetOptions());
    }

    [Fact]
    public void Stored_object_field_transformer_resolves_to_one_instance_across_connection_scopes()
    {
        var container = BuildContainer();
        using var scopeA = container.BeginLifetimeScope();
        using var scopeB = container.BeginLifetimeScope();

        scopeA.Resolve<IStoredObjectFieldTransformer>()
            .Should().BeSameAs(scopeB.Resolve<IStoredObjectFieldTransformer>());
    }

    [Fact]
    public void Every_message_handler_resolves_keyed_by_its_message_type()
    {
        var container = BuildContainer(b =>
        {
            b.RegisterInstance(Mock.Of<IConnectionOutput>()).As<IConnectionOutput>();
            b.RegisterInstance(Mock.Of<IConnectionInput>()).As<IConnectionInput>();
            b.RegisterInstance(new TestOutputLoggerFactory()).As<ILoggerFactory>();
            b.RegisterInstance(new ConfigurationBuilder().Build()).As<IConfiguration>();
        });

        using var scope = container.BeginLifetimeScope();

        var handlerTypes = typeof(BackendModule).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IMessageHandler)))
            .ToArray();

        handlerTypes.Should().NotBeEmpty();

        var handlers = scope.Resolve<IIndex<Type, IMessageHandler>>();

        foreach (var handlerType in handlerTypes)
        {
            var messageType = MessageHandlingHelper.MessageTypeFor(handlerType);
            handlers.TryGetValue(messageType, out var handler)
                .Should()
                .BeTrue($"{handlerType.Name} should be resolvable for {messageType.Name}");

            handler.Should().BeOfType(handlerType);
        }
    }

    [Fact]
    public void Protocol_messages_are_not_registered_as_services()
    {
        var container = BuildContainer();

        container.IsRegistered<IProtocolMessage>().Should().BeFalse();
    }

    [Fact]
    public void Classes_injecting_a_plain_ILogger_get_one_categorised_by_their_own_type()
    {
        var factory = new Mock<ILoggerFactory>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);
        var container = BuildContainer(b =>
        {
            b.RegisterInstance(Mock.Of<IConnectionOutput>()).As<IConnectionOutput>();
            b.RegisterInstance(Mock.Of<IConnectionInput>()).As<IConnectionInput>();
            b.RegisterInstance(factory.Object).As<ILoggerFactory>();
            b.RegisterInstance(new ConfigurationBuilder().Build()).As<IConfiguration>();
        });
        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IIndex<Type, IMessageHandler>>().TryGetValue(typeof(NewDriverRequest), out _);

        factory.Verify(f => f.CreateLogger(typeof(NewDriverHandler).FullName!));
        factory.Verify(f => f.CreateLogger(typeof(ResponseWriter).FullName!));
    }

    [Fact]
    public async Task A_stored_logging_disposable_logs_exactly_once_when_the_store_is_cleared()
    {
        var loggerFactory = new CountingLoggerFactory();
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(loggerFactory));
        var scope = container.BeginLifetimeScope();

        var creator = scope.Resolve<LoggingDisposableCreator>();
        var endTestLogger = creator("Test closedown", "END TEST marker");
        var store = scope.Resolve<IObjectStore>();
        store.Store(endTestLogger);

        await store.ClearAsync();
        await scope.DisposeAsync();

        loggerFactory.MessageCount("END TEST marker").Should().Be(1);
    }

    private class CountingLoggerFactory : ILoggerFactory
    {
        private readonly List<string> _messages = [];

        public int MessageCount(string message)
        {
            return _messages.Count(m => m == message);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CountingLogger(_messages);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        private class CountingLogger : ILogger
        {
            private readonly List<string> _messages;

            public CountingLogger(List<string> messages)
            {
                _messages = messages;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (_messages)
                {
                    _messages.Add(formatter(state, exception));
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }
        }
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
    public void Object_store_resolves_to_one_instance_across_connection_scopes()
    {
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(new TestOutputLoggerFactory()));
        using var scopeA = container.BeginLifetimeScope();
        using var scopeB = container.BeginLifetimeScope();

        scopeA.Resolve<IObjectStore>().Should().BeSameAs(scopeB.Resolve<IObjectStore>());
    }

    [Fact]
    public async Task Handles_stored_before_a_clear_do_not_resolve_after_it()
    {
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(new TestOutputLoggerFactory()));
        using var scope = container.BeginLifetimeScope();

        var store = scope.Resolve<IObjectStore>();
        var id = store.Store(new Thing());
        await store.ClearAsync();
        var options = scope.Resolve<IJsonOptionsProvider>().GetOptions();

        var act = () => JsonSerializer.Deserialize<Request>($$"""{"thing":"{{id}}"}""", options);

        act.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void IDateTimeProvider_resolves_to_the_backends_wrapper_not_a_types_own_private_nested_class()
    {
        var container = BuildContainer();
        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IDateTimeProvider>().Should().BeOfType<CurrentDateTimeProvider>();
    }

    [Fact]
    public void Json_options_provider_resolves_to_the_same_instance_within_a_connection_scope()
    {
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(new TestOutputLoggerFactory()));
        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IJsonOptionsProvider>().Should().BeSameAs(scope.Resolve<IJsonOptionsProvider>());
    }

    [Fact]
    public void Message_serializer_resolves_to_the_same_instance_within_a_connection_scope()
    {
        var container = BuildContainer(b => b.RegisterInstance<ILoggerFactory>(new TestOutputLoggerFactory()));
        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IMessageSerializer>().Should().BeSameAs(scope.Resolve<IMessageSerializer>());
    }

    [Fact]
    public void Response_writer_resolves_to_the_same_instance_within_a_connection_scope()
    {
        var container = BuildContainer(b =>
        {
            b.RegisterInstance(Mock.Of<IConnectionOutput>()).As<IConnectionOutput>();
            b.RegisterInstance(new TestOutputLoggerFactory()).As<ILoggerFactory>();
        });
        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IResponseWriter>().Should().BeSameAs(scope.Resolve<IResponseWriter>());
    }

    private record Request
    {
        [StoredObject]
        public Thing Thing { get; init; } = null!;
    }

    private class Thing;
}
