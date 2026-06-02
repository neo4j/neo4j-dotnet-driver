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
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.DependencyInjection;

internal class ScopedContainerRewrite : IResolutionScope, IServiceRegistry, IDisposable
{
    private readonly ScopedContainerRewrite? _outerScope;
    private readonly MultiMap<Type, Registration> _registrations = new();
    private readonly List<IResolverOverride> _overrides = [];

    public ScopedContainerRewrite() : this(null)
    {
    }

    private ScopedContainerRewrite(ScopedContainerRewrite? outerScope)
    {
        _outerScope = outerScope;
        RegisterInstance<IResolutionScope>(this);
    }

    public IServiceRegistry RegisterInstance<TService>(TService instance)
    {
        var type = typeof(TService);
        _registrations[type].Add(Registration.FromInstance(instance!));
        return this;
    }

    public IServiceRegistry RegisterType<TService>() => RegisterType<TService, TService>();

    public IServiceRegistry RegisterType<TService, TImplementation>() where TImplementation : TService
        => RegisterType(typeof(TService), typeof(TImplementation));

    public IServiceRegistry RegisterType(Type service, Type implementation)
    {
        _registrations[service].Add(Registration.FromType(implementation));
        return this;
    }

    public IServiceRegistry RegisterPlugin(IResolverOverride resolverOverride)
    {
        _overrides.Add(resolverOverride);
        return this;
    }

    private IEnumerable<object> ResolveAll(
        Type serviceType,
        ScopedContainerRewrite? childScope,
        HashSet<Type> resolutionStack)
    {
        var local = _registrations.GetEnumerable(serviceType);
        foreach (var reg in local.Reverse())
        {
            yield return CreateInstance(reg, childScope, resolutionStack);
        }

        // break recursion if no outer scope
        if (_outerScope == null)
        {
            yield break;
        }

        foreach (var obj in _outerScope.ResolveAll(serviceType, childScope, resolutionStack))
        {
            yield return obj;
        }
    }

    private object CreateInstance(
        Registration registration,
        ScopedContainerRewrite? childScope,
        HashSet<Type> resolutionStack)
    {
        if (registration.Instance is not null)
        {
            return registration.Instance;
        }

        var implementationType = registration.ImplementationType;

        // if adding fails, it's already in there 
        if (!resolutionStack.Add(implementationType))
        {
            var stack = string.Join(" > ", resolutionStack);
            throw new InvalidOperationException(
                $"Circular dependency detected while constructing {implementationType.Name}. " +
                $"Resolution stack: {stack}.");
        }

        try
        {
            var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"Type {implementationType.Name} has no public constructors.");
            }

            var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var nullabilityCtx = new NullabilityInfoContext();
            var parametersWithNullability = ctor.GetParameters()
                .Select(p => (p, nullabilityCtx.Create(p).WriteState == NullabilityState.Nullable))
                .ToList();

            var args = new List<object?>(parametersWithNullability.Count);
            var resolver = childScope ?? this;
            foreach (var (p, nullable) in parametersWithNullability)
            {
                var resolved = resolver.ResolveAll(p.ParameterType, childScope, resolutionStack).FirstOrDefault();
                var arg = resolved switch
                {
                    not null => resolved,
                    null when nullable => null,
                    _ => throw new InvalidOperationException(
                        $"No service of type {p.ParameterType} has been registered.")
                };

                args.Add(arg);
            }

            return Activator.CreateInstance(implementationType, args.ToArray())!;
        }
        finally
        {
            resolutionStack.Remove(implementationType);
        }
    }

    public TService Resolve<TService>() => (TService)Resolve(typeof(TService));

    public object Resolve(Type serviceType) => Resolve(serviceType, null!);

    public object Resolve(Type serviceType, Type requestingType)
    {
        return TryResolve(serviceType, out var service)
            ? service
            : throw new InvalidOperationException($"No service of type {serviceType} has been registered.");
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
        var resolutionStack = new HashSet<Type>();

        if (serviceType.IsGenericIEnumerable())
        {
            var elementType = serviceType.GetGenericArguments()[0];
            var instances = ResolveAll(elementType, this, resolutionStack).ToArray();
            var array = Array.CreateInstance(elementType, instances.Length);
            instances.CopyTo(array, 0);
            service = array;
            return true;
        }

        service = ResolveAll(serviceType, this, resolutionStack).FirstOrDefault();
        return service is not null;
    }

    public void Dispose() { }

    public IResolutionScope CreateChildScope(Action<IServiceRegistry> registrations)
    {
        var child = new ScopedContainerRewrite(this);
        registrations(child);
        return child;
    }

    private class Registration
    {
        private Registration(object? instance, Type implementationType)
        {
            Instance = instance;
            ImplementationType = implementationType;
        }

        public static Registration FromInstance(object instance)
            => new(instance, null!);

        public static Registration FromType(Type implementationType)
            => new(null, implementationType);

        public object? Instance { get; }
        public Type ImplementationType { get; }
    }
}
