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

namespace Neo4j.Driver.Bolt.TestHarness.DependencyInjection;

/// <summary>
/// Minimal service resolver for harness experiments (instance + transient registrations only).
/// </summary>
public sealed class SimpleServiceResolver
{
    private readonly Dictionary<Type, Type> _implementations = new();
    private readonly Dictionary<Type, object> _instances = new();

    public void Register<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _implementations[typeof(TService)] = typeof(TImplementation);
    }

    public void RegisterInstance<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _instances[typeof(TService)] = instance;
    }

    public TService Resolve<TService>()
        where TService : class =>
        (TService)Resolve(typeof(TService), new Stack<Type>());

    private object Resolve(Type serviceType, Stack<Type> resolutionStack)
    {
        if (_instances.TryGetValue(serviceType, out var existing))
        {
            return existing;
        }

        if (!_implementations.TryGetValue(serviceType, out var implementationType))
        {
            throw new InvalidOperationException($"No registration for {serviceType.Name}.");
        }

        if (resolutionStack.Contains(implementationType))
        {
            throw new InvalidOperationException($"Circular dependency while resolving {implementationType.Name}.");
        }

        resolutionStack.Push(implementationType);
        try
        {
            var ctor = SelectConstructor(implementationType);
            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = Resolve(parameters[i].ParameterType, resolutionStack);
            }

            return ctor.Invoke(args);
        }
        finally
        {
            resolutionStack.Pop();
        }
    }

    private static ConstructorInfo SelectConstructor(Type implementationType)
    {
        var constructors = implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        if (constructors.Length == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} has no public constructors.");
        }

        if (constructors.Length == 1)
        {
            return constructors[0];
        }

        return constructors.MaxBy(c => c.GetParameters().Length)
            ?? throw new InvalidOperationException($"{implementationType.Name} has no public constructors.");
    }
}
