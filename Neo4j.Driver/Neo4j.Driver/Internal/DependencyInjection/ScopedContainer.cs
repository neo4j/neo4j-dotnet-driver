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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Neo4j.Driver.Internal.DependencyInjection;

internal class ScopedContainer : IResolutionScope, IServiceRegistry, IDisposable
{
    private readonly HashSet<object> _disposables = [];
    private readonly List<IResolverOverride> _overrides = [];
    private readonly ScopedContainer? _parent;
    private readonly Dictionary<Type, List<Registration>> _registrations = new();
    private readonly ThreadLocal<HashSet<Type>> _resolutionStack = new(() => []);
    private bool _disposed;

    public ScopedContainer() : this(null)
    {
    }

    private ScopedContainer(ScopedContainer? parent)
    {
        _parent = parent;
        RegisterInstance<IResolutionScope>(this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var disposable in _disposables)
        {
            if (disposable is IDisposable d)
            {
                d.Dispose();
            }
        }

        _disposables.Clear();
        _resolutionStack.Dispose();
    }

    public TService Resolve<TService>()
    {
        return (TService)ResolveCore(typeof(TService), null, null);
    }

    public object Resolve(Type serviceType)
    {
        return ResolveCore(serviceType, null, null);
    }

    public object Resolve(Type serviceType, Type? requestingType)
    {
        return ResolveCore(serviceType, requestingType, null);
    }

    public bool TryResolve<T>(out T? value)
    {
        var result = TryResolveCore(typeof(T), null, null);
        if (result is null)
        {
            value = default;
            return false;
        }

        value = (T)result;
        return true;
    }

    public IResolutionScope CreateChildScope(Action<IServiceRegistry> registrations)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var newScope = new ScopedContainer(this);
        registrations(newScope);
        return newScope;
    }

    public IServiceRegistry RegisterInstance<TService>(TService instance)
    {
        if (instance == null)
        {
            throw new ArgumentException("Instance cannot be null", nameof(instance));
        }

        var serviceType = typeof(TService);
        if (!_registrations.TryGetValue(serviceType, out var registrations))
        {
            registrations = [];
            _registrations[serviceType] = registrations;
        }

        registrations.Add(new Registration(instance));
        return this;
    }

    public IServiceRegistry RegisterType<TService, TImplementation>() where TImplementation : TService
    {
        return RegisterType(typeof(TService), typeof(TImplementation));
    }

    public IServiceRegistry RegisterType(Type service, Type implementation)
    {
        if (!_registrations.TryGetValue(service, out var registrations))
        {
            registrations = [];
            _registrations[service] = registrations;
        }

        registrations.Add(new Registration(implementation));
        return this;
    }

    public IServiceRegistry RegisterType<TService>()
    {
        var serviceType = typeof(TService);
        if (!_registrations.TryGetValue(serviceType, out var registrations))
        {
            registrations = [];
            _registrations[serviceType] = registrations;
        }

        registrations.Add(new Registration(serviceType));
        return this;
    }

    public IServiceRegistry RegisterPlugin(IResolverOverride resolverOverride)
    {
        _overrides.Add(resolverOverride);
        return this;
    }

    // Core resolution method. extraRegistrations carries the calling child scope's
    // registrations so they take priority throughout the entire resolution chain,
    // including transitive dependencies instantiated by ancestor scopes.
    private object ResolveCore(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? extraRegistrations)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopedContainer));
        }

        if (_resolutionStack.Value != null && !_resolutionStack.Value.Add(serviceType))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {serviceType}. " +
                $"Resolution chain: {string.Join(" -> ", _resolutionStack.Value.Select(t => t.Name))}");
        }

        try
        {
            // Child-scope registrations take priority over everything in the parent chain.
            if (extraRegistrations != null
                && extraRegistrations.TryGetValue(serviceType, out var extraRegs)
                && extraRegs.Count > 0)
            {
                var reg = extraRegs[^1];
                return reg.Instance ?? CreateInstance(reg.ImplementationType, serviceType, extraRegistrations);
            }

            // Plugin overrides
            foreach (var resolverOverride in _overrides)
            {
                if (resolverOverride.TryResolve(serviceType, requestingType, this, out var overrideResult))
                {
                    return overrideResult;
                }
            }

            // IEnumerable<T>
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                return ResolveEnumerable(elementType, extraRegistrations);
            }

            // IScoped<T> — capture the leaf scope and wrap it for lazy optional resolution
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IScoped<>))
            {
                var innerType = serviceType.GetGenericArguments()[0];
                var scope = (IResolutionScope)ResolveCore(typeof(IResolutionScope), requestingType, extraRegistrations);
                return Activator.CreateInstance(typeof(Scoped<>).MakeGenericType(innerType), scope)!;
            }

            // Local registrations
            if (_registrations.TryGetValue(serviceType, out var registrations) && registrations.Count > 0)
            {
                var registration = registrations[^1];
                return registration.Instance ?? CreateInstance(registration.ImplementationType, serviceType, extraRegistrations);
            }

            // Delegate to parent. Merge this scope's registrations with whatever the child
            // passed down, so every ancestor scope sees registrations from the full chain.
            // The more-derived scope's registrations win for any conflicting key.
            if (_parent != null)
            {
                return _parent.ResolveCore(serviceType, requestingType, MergeRegistrations(_registrations, extraRegistrations));
            }

            throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
        }
        finally
        {
            _resolutionStack.Value?.Remove(serviceType);
        }
    }

    // Like ResolveCore but returns null instead of throwing when a service is not registered.
    // Other failure modes (disposed, circular dependency, construction failure) still throw.
    private object? TryResolveCore(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? extraRegistrations)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopedContainer));
        }

        if (_resolutionStack.Value != null && !_resolutionStack.Value.Add(serviceType))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {serviceType}. " +
                $"Resolution chain: {string.Join(" -> ", _resolutionStack.Value.Select(t => t.Name))}");
        }

        try
        {
            if (extraRegistrations != null
                && extraRegistrations.TryGetValue(serviceType, out var extraRegs)
                && extraRegs.Count > 0)
            {
                var reg = extraRegs[^1];
                return reg.Instance ?? CreateInstance(reg.ImplementationType, serviceType, extraRegistrations);
            }

            foreach (var resolverOverride in _overrides)
            {
                if (resolverOverride.TryResolve(serviceType, requestingType, this, out var overrideResult))
                {
                    return overrideResult;
                }
            }

            if (_registrations.TryGetValue(serviceType, out var registrations) && registrations.Count > 0)
            {
                var registration = registrations[^1];
                return registration.Instance ?? CreateInstance(registration.ImplementationType, serviceType, extraRegistrations);
            }

            if (_parent != null)
            {
                return _parent.TryResolveCore(serviceType, requestingType, MergeRegistrations(_registrations, extraRegistrations));
            }

            return null;
        }
        finally
        {
            _resolutionStack.Value?.Remove(serviceType);
        }
    }

    private Array ResolveEnumerable(Type elementType, Dictionary<Type, List<Registration>>? extraRegistrations)
    {
        var instances = new List<object>();

        // Merge this scope's registrations with any child overrides passed in.
        // The more-derived scope's registrations win for conflicting keys.
        var effectiveOverrides = MergeRegistrations(_registrations, extraRegistrations);

        // Collect from parent first, forwarding the effective overrides so that
        // parent-registered implementations are instantiated with child-scope deps.
        if (_parent != null)
        {
            try
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
                var parentEnumerable = _parent.ResolveCore(enumerableType, null, effectiveOverrides);
                if (parentEnumerable is IEnumerable enumerable)
                {
                    instances.AddRange(enumerable.Cast<object>());
                }
            }
            catch (InvalidOperationException)
            {
                // Parent has no registrations for this element type — continue.
            }
        }

        // Add implementations from this scope's own registrations.
        foreach (var (type, registrations) in _registrations)
        {
            if (elementType.IsAssignableFrom(type))
            {
                foreach (var registration in registrations)
                {
                    instances.Add(registration.Instance ?? CreateInstance(registration.ImplementationType, type, effectiveOverrides));
                }
            }
        }

        var array = Array.CreateInstance(elementType, instances.Count);
        for (var i = 0; i < instances.Count; i++)
        {
            array.SetValue(instances[i], i);
        }

        return array;
    }

    private object CreateInstance(
        Type implementationType,
        Type requestingType,
        Dictionary<Type, List<Registration>>? extraRegistrations)
    {
        var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        if (constructors.Length == 0)
        {
            throw new InvalidOperationException($"Type {implementationType} has no public constructors.");
        }

        var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = constructor.GetParameters();
        var parameterInstances = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            parameterInstances[i] = ResolveCore(parameters[i].ParameterType, requestingType, extraRegistrations);
        }

        var instance = Activator.CreateInstance(implementationType, parameterInstances);

        if (instance is null)
        {
            throw new InvalidOperationException($"Failed to create instance of type {implementationType}.");
        }

        if (instance is IDisposable)
        {
            _disposables.Add(instance);
        }

        if (instance is IScopeAware scopeAware)
        {
            scopeAware.OnResolved(this);
        }

        return instance;
    }

    // Returns a new dictionary containing all entries from base, with entries from
    // overrides winning for any conflicting key. Returns base unchanged if overrides
    // is null or empty (avoids allocation on the common non-nested path).
    private static Dictionary<Type, List<Registration>> MergeRegistrations(
        Dictionary<Type, List<Registration>> baseRegistrations,
        Dictionary<Type, List<Registration>>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
        {
            return baseRegistrations;
        }

        var merged = new Dictionary<Type, List<Registration>>(baseRegistrations);
        foreach (var (type, registrations) in overrides)
        {
            merged[type] = registrations;
        }

        return merged;
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
}
