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
using Neo4j.Driver.TestKitBackend.Protocol;
using Module = Autofac.Module;

namespace Neo4j.Driver.TestKitBackend;

internal class BackendModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var handlerTypes = new MessageHandlerTypesProvider().GetTypes();
        RegisterHandlersKeyedByMessageType(builder, handlerTypes);

        builder
            .RegisterAssemblyTypes(assembly)
            .Where(t => !handlerTypes.Contains(t) && LifetimeOf(t) == RegistrationLifetime.PerDependency)
            .AsImplementedInterfaces()
            .InstancePerDependency();

        builder
            .RegisterAssemblyTypes(assembly)
            .Where(t => LifetimeOf(t) == RegistrationLifetime.PerLifetimeScope)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder
            .RegisterAssemblyTypes(assembly)
            .Where(t => LifetimeOf(t) == RegistrationLifetime.Singleton)
            .AsImplementedInterfaces()
            .SingleInstance();
    }

    private static RegistrationLifetime LifetimeOf(Type type)
    {
        return type.GetCustomAttribute<RegistrationLifetimeAttribute>()?.Lifetime
            ?? RegistrationLifetime.PerDependency;
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
