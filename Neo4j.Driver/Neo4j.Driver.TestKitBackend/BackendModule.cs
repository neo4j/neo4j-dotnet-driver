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

public class BackendModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = typeof(BackendModule).Assembly;

        var messageTypes = DoMessageDiscoveryRegistration(builder, assembly, out var handlerTypes);

        var singletons = new List<object> {
            new MessageTypeMap(messageTypes)
        };

        builder
            .RegisterAssemblyTypes(assembly)
            .Where(t => singletons.All(s => s.GetType() != t) && !handlerTypes.Contains(t))
            .AsImplementedInterfaces()
            .InstancePerDependency();

        foreach (var singleton in singletons)
        {
            builder.RegisterInstance(singleton).AsImplementedInterfaces();
        }
    }

    private static IEnumerable<Type> DoMessageDiscoveryRegistration(
        ContainerBuilder builder,
        Assembly assembly,
        out List<Type> handlerTypes)
    {
        var messageTypes = assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                typeof(IProtocolMessage).IsAssignableFrom(t));

        handlerTypes = assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                typeof(IMessageHandler).IsAssignableFrom(t))
            .ToList();

        // Key each handler by the message type it handles so the dispatcher can resolve it via
        // IIndex<Type, IMessageHandler>[message.GetType()].
        foreach (var handlerType in handlerTypes)
        {
            builder
                .RegisterType(handlerType)
                .Keyed<IMessageHandler>(MessageHandlingHelper.MessageTypeFor(handlerType))
                .InstancePerDependency();
        }

        return messageTypes;
    }
}
