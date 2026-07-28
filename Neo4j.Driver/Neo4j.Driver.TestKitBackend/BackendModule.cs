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
using Module = Autofac.Module;

namespace Neo4j.Driver.TestKitBackend;

internal class BackendModule : Module
{
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
    }

    private static IEnumerable<Type> RegisterableTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });
    }

    private static RegistrationLifetime LifetimeOf(Type type)
    {
        return type.GetCustomAttribute<RegistrationLifetimeAttribute>()?.Lifetime 
         ?? RegistrationLifetime.PerDependency;
    }
}
