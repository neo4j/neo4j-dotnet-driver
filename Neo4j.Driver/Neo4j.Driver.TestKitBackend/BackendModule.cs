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

using System.Reflection;
using Autofac;
using Neo4j.Driver.TestKitBackend.Logging;
using Neo4j.Driver.TestKitBackend.Protocol;
using Module = Autofac.Module;

namespace Neo4j.Driver.TestKitBackend;

internal class BackendModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var handlerTypes = ConcreteImplementationsOf<IMessageHandler>(assembly);
        RegisterHandlersKeyedByMessageType(builder, handlerTypes);

        var messageTypes = ConcreteImplementationsOf<IProtocolMessage>(assembly);
        var wireNameProvider = new WireNameProvider();
        var singletons = new List<object>
        {
            wireNameProvider,
            new MessageTypeMap(messageTypes, wireNameProvider),
            new ConnectionIdProvider(),
            new LoggingContextAccessor()
        };

        var scopedTypes = new[] { typeof(LoggingContext), typeof(Registry) };

        builder
            .RegisterAssemblyTypes(assembly)
            .Where(t => singletons.All(s => s.GetType() != t) && !handlerTypes.Contains(t) && !scopedTypes.Contains(t))
            .AsImplementedInterfaces()
            .InstancePerDependency();

        // One logging context per connection scope; handlers mutate it, the connection handler
        // publishes it to the accessor so the process-wide enricher can find it.
        builder.RegisterType<LoggingContext>().As<ILoggingContext>().InstancePerLifetimeScope();

        // One registry per connection scope, so handlers and the handle converters share it and
        // handle IDs can never resolve across tests.
        builder.RegisterType<Registry>().As<IRegistry>().InstancePerLifetimeScope();

        foreach (var singleton in singletons)
        {
            builder.RegisterInstance(singleton).AsImplementedInterfaces();
        }
    }

    private static Type[] ConcreteImplementationsOf<T>(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                typeof(T).IsAssignableFrom(t))
            .ToArray();
    }

    // Key each handler by the message type it handles so the dispatcher can resolve it via
    // IIndex<Type, IMessageHandler>[message.GetType()].
    private static void RegisterHandlersKeyedByMessageType(ContainerBuilder builder, Type[] handlerTypes)
    {
        foreach (var handlerType in handlerTypes)
        {
            builder
                .RegisterType(handlerType)
                .Keyed<IMessageHandler>(MessageHandlingHelper.MessageTypeFor(handlerType))
                .InstancePerDependency();
        }
    }
}
