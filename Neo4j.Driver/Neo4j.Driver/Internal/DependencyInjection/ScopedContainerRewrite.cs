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

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.DependencyInjection;

internal class ScopedContainerRewrite : IResolutionScope
{
    private readonly ScopedContainerRewrite? _outerScope;
    private readonly Dictionary<Type, List<Registration>> _registrations = new();
    private readonly ThreadLocal<HashSet<Type>> _resolutionStack = new(() => []);

    private Func<object>[] GetFactories(
        Type serviceType,
        ScopedContainerRewrite? childScope = null)
    {
        if (!_resolutionStack.Value!.Add(serviceType))
        {
            return [];
        }

        try
        {
            var factories = new List<Func<object>>();
            if (_outerScope != null)
            {
                factories.AddRange(_outerScope.GetFactories(serviceType, childScope));
            }

            if (_registrations.TryGetValue(serviceType, out var local))
            {
                factories.AddRange(local.Select(r => MakeFactory(r, childScope)));
            }

            if (childScope != null)
            {
                factories.AddRange(childScope.GetFactories(serviceType, childScope));
            }

            return [.. factories];
        }
        finally
        {
            _resolutionStack.Value.Remove(serviceType);
        }
    }

    private Func<object> MakeFactory(Registration registration, ScopedContainerRewrite? childScope)
    {
        if (registration.Instance is not null)
        {
            return () => registration.Instance;
        }

        var implementationType = registration.ImplementationType;
        var child = childScope;

        var ctor = implementationType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var parameters = ctor.GetParameters();
        var nullabilityCtx = new NullabilityInfoContext();
        var parametersWithNullability = parameters
            .Select(p => (p, nullabilityCtx.Create(p).WriteState == NullabilityState.Nullable))
            .ToList();

        return () =>
        {
            var args = new List<object?>(parametersWithNullability.Count);
            foreach (var (parameter, nullable) in parametersWithNullability)
            {
                var factories = GetFactories(parameter.ParameterType, child);
                var arg = factories.Length switch
                {
                    > 0 => factories[^1](),
                    0 when nullable => null,
                    _ => throw new InvalidOperationException(
                        $"No service of type {parameter.ParameterType} has been registered.")
                };
                
                args.Add(arg);
            }

            return Activator.CreateInstance(implementationType, args.ToArray())!;
        };
    }

    private class Registration
    {
        public Registration(object instance)
        {
            Instance = instance;
            ImplementationType = null!;
        }

        public Registration(Type implementationType)
        {
            Instance = null;
            ImplementationType = implementationType;
        }

        public object? Instance { get; }
        public Type ImplementationType { get; }
    }

    public TService Resolve<TService>() => (TService)Resolve(typeof(TService));

    public object Resolve(Type serviceType) => Resolve(serviceType, null!);

    public object Resolve(Type serviceType, Type requestingType)
    {
        if (TryResolve(serviceType, out var service))
        {
            return service;
        }

        throw new InvalidOperationException($"No service of type {serviceType} has been registered.");
    }

    public bool TryResolve<T>([NotNullWhen(true)] out T? value)
    {
        if (TryResolve(typeof(T), out var resolved) && resolved is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryResolve(Type serviceType, [NotNullWhen(true)] out object? service)
    {
        if(serviceType.IsGenericIEnumerable())
        {
            var elementType = serviceType.GetGenericArguments()[0];
            var elementFactories = GetFactories(elementType, this);
            var array = Array.CreateInstance(elementType, elementFactories.Length);
            var items = elementFactories.Select(f => f());
            items.ToArray().CopyTo(array, 0);
            service = array;
            return true;
        }

        var factories = GetFactories(serviceType, this);
        if (factories.Length == 0)
        {
            service = null;
            return false;
        }

        service = factories[^1]();
        return true;
    }

    public IResolutionScope CreateChildScope(Action<IServiceRegistry> registrations)
    {
        throw new NotImplementedException();
    }
}
