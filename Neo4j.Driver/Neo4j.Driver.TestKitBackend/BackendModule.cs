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
using Autofac.Core;
using Autofac.Core.Registration;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Infrastructure;
using Module = Autofac.Module;

namespace Neo4j.Driver.TestKitBackend;

internal class BackendModule : Module
{
    private static readonly LoggerMiddleware LoggerMiddleware = new();

    protected override void Load(ContainerBuilder builder)
    {
        foreach (var type in RegisterableTypes(Assembly.GetExecutingAssembly()))
        {
            var registration = builder.RegisterType(type).AsImplementedInterfaces();

            _ = LifetimeOf(type) switch
            {
                RegistrationLifetime.PerDependency => registration.InstancePerDependency(),
                RegistrationLifetime.PerLifetimeScope => registration.InstancePerLifetimeScope(),
                RegistrationLifetime.Singleton => registration.SingleInstance(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        foreach (var completionType in RegisterableTypes(Assembly.GetExecutingAssembly())
                     .Where(t => t.IsAssignableTo(typeof(ICallbackCompletion))))
        {
            builder.RegisterType(typeof(CallbackCompletedHandler<>).MakeGenericType(completionType))
                .AsImplementedInterfaces();
        }

        builder
            .Register((_, parameters) => new ConnectionInput(new LineReader(parameters.TypedAs<TextReader>())))
            .As<IConnectionInput>();
    }

    protected override void AttachToComponentRegistration(
        IComponentRegistryBuilder componentRegistry,
        IComponentRegistration registration)
    {
        registration.PipelineBuilding += (_, pipeline) => pipeline.Use(LoggerMiddleware);
    }

    private static IEnumerable<Type> RegisterableTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
                !t.IsAssignableTo(typeof(Delegate)));
    }

    private static RegistrationLifetime LifetimeOf(Type type)
    {
        return type.GetCustomAttribute<RegistrationLifetimeAttribute>()?.Lifetime 
         ?? RegistrationLifetime.PerDependency;
    }
}
