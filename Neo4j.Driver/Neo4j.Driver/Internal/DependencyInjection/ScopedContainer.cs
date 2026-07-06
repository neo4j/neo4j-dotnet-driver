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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.DependencyInjection;

internal class ScopedContainer : IResolutionScope, IServiceRegistry, IDisposable
{
    private readonly ConcurrentDictionary<Type, (ConstructorInfo Constructor, ParameterInfo[] Parameters)> _constructorCache = new();
    private readonly Stack<IAsyncDisposable> _disposables = new();
    private readonly List<IResolutionInterceptor> _interceptors = [];
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

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        while (_disposables.TryPop(out var disposable))
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }

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

    public bool TryResolve<T>([NotNullWhen(true)] out T? value)
    {
        var success = TryResolve(typeof(T), out var service);
        value = (T?)service;
        return success;
    }

    public bool TryResolve(Type serviceType, [NotNullWhen(true)] out object? service)
    {
        service = TryResolveCore(serviceType, null, null);
        return service != null;
    }

    public IResolutionScope CreateChildScope(Action<IServiceRegistry> registrations)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var newScope = new ScopedContainer(this);
        registrations(newScope);
        TrackInstanceForDisposal(newScope);
        return newScope;
    }

    public IServiceRegistry RegisterInstance<TService>(TService instance, bool transferOwnership = false)
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

        if (transferOwnership)
        {
            TrackInstanceForDisposal(instance);
        }

        return this;
    }

    private void TrackInstanceForDisposal<TService>([DisallowNull] TService instance)
    {
        switch (instance)
        {
            case IAsyncDisposable ad: _disposables.Push(ad); break;
            case IDisposable d: _disposables.Push(new AsyncDisposalWrapper(d)); break;
        }
    }

    public IServiceRegistry RegisterType<TService, TImplementation>(bool singleton = false)
        where TImplementation : TService
    {
        return RegisterType(typeof(TService), typeof(TImplementation), singleton);
    }

    public IServiceRegistry RegisterType(Type service, Type implementation, bool singleton = false)
    {
        if (!_registrations.TryGetValue(service, out var registrations))
        {
            registrations = [];
            _registrations[service] = registrations;
        }

        registrations.Add(new Registration(implementation, singleton));
        return this;
    }

    public IServiceRegistry RegisterType<TService>(bool singleton = false)
    {
        var serviceType = typeof(TService);
        if (!_registrations.TryGetValue(serviceType, out var registrations))
        {
            registrations = [];
            _registrations[serviceType] = registrations;
        }

        registrations.Add(new Registration(serviceType, singleton));
        return this;
    }

    public IServiceRegistry RegisterInterceptor(IResolutionInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
        return this;
    }

    private object ResolveCore(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? extraRegistrations,
        ScopedContainer? childScope = null)
    {
        return TryResolveCore(serviceType, requestingType, extraRegistrations, childScope) ??
            throw new InvalidOperationException(
                $"Service of type {serviceType} required by {requestingType} is not registered. " +
                $"Resolution chain: {GetResolutionStackString()}");
    }

    // All resolution logic lives here. Returns null only when the service is not registered.
    // Other failure modes (disposed, circular dependency, construction failure) still throw.
    // extraRegistrations carries the calling child scope's registrations so they take priority
    // throughout the entire resolution chain, including transitive dependencies resolved by ancestors.
    private object? TryResolveCore(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopedContainer));
        }

        if (_resolutionStack.Value != null && !_resolutionStack.Value.Add(serviceType))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {serviceType}. " +
                $"Resolution chain: {GetResolutionStackString()}");
        }

        try
        {
            // Child-scope registrations take priority over everything in the parent chain.
            if (childRegistrations != null &&
                childRegistrations.TryGetValue(serviceType, out var foundInChild) &&
                foundInChild.Count > 0)
            {
                var reg = foundInChild[^1];
                if (reg.Instance != null)
                {
                    return reg.Instance;
                }

                if (reg.Singleton && childScope != null)
                {
                    // singletons should be attached to innermost scope
                    return childScope.ResolveCore(serviceType, requestingType, null, null);
                }

                return CreateInstance(reg.ImplementationType, serviceType, childRegistrations, childScope);
            }

            // Interceptors
            var interceptorResolver = (IServiceResolver)(childScope ?? this);
            foreach (var interceptor in _interceptors)
            {
                if (interceptor.TryResolve(serviceType, requestingType, interceptorResolver, out var overrideResult))
                {
                    return overrideResult;
                }
            }

            // IEnumerable<T>
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                return ResolveEnumerable(elementType, childRegistrations, childScope);
            }

            // Local registrations
            if (_registrations.TryGetValue(serviceType, out var registrations) && registrations.Count > 0)
            {
                var registration = registrations[^1];
                if (registration.Instance != null)
                {
                    return registration.Instance;
                }

                var instance = CreateInstance(
                    registration.ImplementationType,
                    serviceType,
                    childRegistrations,
                    childScope);

                if (registration.Singleton)
                {
                    registration.Instance = instance;
                }

                return instance;
            }

            // Delegate to parent. Merge this scope's registrations with whatever the child
            // passed down, so every ancestor scope sees registrations from the full chain.
            // The more-derived scope's registrations win for any conflicting key.
            if (_parent != null)
            {
                return _parent.TryResolveCore(
                    serviceType,
                    requestingType,
                    MergeRegistrations(_registrations, childRegistrations),
                    childScope ?? this);
            }

            return null;
        }
        finally
        {
            _resolutionStack.Value?.Remove(serviceType);
        }
    }

    private string GetResolutionStackString()
    {
        return string.Join(" -> ", _resolutionStack.Value?.Select(t => t.Name) ?? []);
    }

    private Array ResolveEnumerable(
        Type elementType,
        Dictionary<Type, List<Registration>>? extraRegistrations,
        ScopedContainer? childScope = null)
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
                var parentEnumerable = _parent.ResolveCore(enumerableType, null, effectiveOverrides, childScope);
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
            if (!elementType.IsAssignableFrom(type))
            {
                continue;
            }

            foreach (var registration in registrations)
            {
                instances.Add(
                    registration.Instance ??
                    CreateInstance(registration.ImplementationType, type, effectiveOverrides, childScope));
            }
        }

        var array = Array.CreateInstance(elementType, instances.Count);
        for (var i = 0; i < instances.Count; i++)
        {
            array.SetValue(instances[i], i);
        }

        return array;
    }

    private (ConstructorInfo Constructor, ParameterInfo[] Parameters) GetConstructorAndParams(Type implementationType)
    {
        return _constructorCache.GetOrAdd(
            implementationType,
            static type =>
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException($"Type {type} has no public constructors.");
                }

                var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                return (constructor, constructor.GetParameters());
            });
    }

    private object CreateInstance(
        Type implementationType,
        Type requestingType,
        Dictionary<Type, List<Registration>>? extraRegistrations,
        ScopedContainer? childScope = null)
    {
        var (constructor, parameters) = GetConstructorAndParams(implementationType);
        var parameterInstances = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            parameterInstances[i] = ResolveCore(
                parameters[i].ParameterType,
                implementationType,
                extraRegistrations,
                childScope);
        }

        var instance = Activator.CreateInstance(implementationType, parameterInstances);

        if (instance is null)
        {
            throw new InvalidOperationException($"Failed to create instance of type {implementationType}.");
        }

        TrackInstanceForDisposal(instance);

        return instance;
    }

    private static Dictionary<Type, List<Registration>> MergeRegistrations(
        Dictionary<Type, List<Registration>> baseRegistrations,
        Dictionary<Type, List<Registration>>? overrides)
    {
        if (overrides is not { Count: > 0 })
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
            Singleton = false;
        }

        public Registration(Type implementationType, bool singleton = false)
        {
            Instance = null;
            ImplementationType = implementationType;
            Singleton = singleton;
        }

        public object? Instance { get; internal set; }
        public Type ImplementationType { get; }
        public bool Singleton { get; }
    }

    private class AsyncDisposalWrapper(IDisposable disposable) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            disposable.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
