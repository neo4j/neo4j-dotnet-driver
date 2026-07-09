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
    private readonly ConcurrentDictionary<Type, (ConstructorInfo Constructor, ParameterInfo[] Parameters)>
        _constructorCache = new();

    private readonly Stack<IAsyncDisposable> _disposables = new();
    private readonly List<IResolutionInterceptor> _interceptors = [];
    private readonly ScopedContainer? _parent;
    private readonly Dictionary<Type, List<Registration>> _registrations = new();
    private readonly ThreadLocal<List<Type>> _resolutionStack = new(() => []);
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(implementation);
        if (implementation.IsAbstract || implementation.IsInterface)
        {
            throw new ArgumentException(
                $"Implementation type {implementation} must be a non-abstract class.",
                nameof(implementation));
        }

        if (!service.IsAssignableFrom(implementation))
        {
            throw new ArgumentException(
                $"Type {implementation} is not assignable to service type {service}.",
                nameof(implementation));
        }

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
        return RegisterType(typeof(TService), typeof(TService), singleton);
    }

    public IServiceRegistry RegisterModule<T>() where T : IRegistrationModule, new()
    {
        var module = new T();
        module.Register(this);
        return this;
    }

    public IServiceRegistry RegisterInterceptor(IResolutionInterceptor interceptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (interceptor == null)
        {
            throw new ArgumentNullException(nameof(interceptor));
        }

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

    private object? TryResolveCore(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ScopedContainer));

        var resolutionStack = _resolutionStack.Value;
        if (resolutionStack != null && resolutionStack.Contains(serviceType))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {serviceType}. " +
                $"Resolution chain: {string.Join(" -> ", resolutionStack.Append(serviceType).Select(t => t.Name))}");
        }

        object? resolved;
        resolutionStack?.Add(serviceType);
        try
        {
            _ = TryResolveFromChildScope(serviceType, requestingType, childRegistrations, childScope, out resolved) ||
                TryApplyInterceptors(serviceType, requestingType, childScope, out resolved) ||
                TryResolveGenericEnumerable(serviceType, childRegistrations, childScope, out resolved) ||
                TryResolveFromLocal(serviceType, childRegistrations, childScope, out resolved) ||
                TryResolveFromParent(serviceType, requestingType, childRegistrations, childScope, out resolved);
        }
        finally
        {
            if (resolutionStack is { Count: > 0 })
            {
                resolutionStack.RemoveAt(resolutionStack.Count - 1);
            }
        }

        return resolved;
    }

    private bool TryResolveFromParent(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope,
        out object? resolved)
    {
        resolved = _parent?.TryResolveCore(
            serviceType,
            requestingType,
            MergeRegistrations(_registrations, childRegistrations),
            childScope ?? this);

        return (resolved is not null);
    }

    private bool TryResolveFromLocal(
        Type serviceType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope,
        out object? resolved)
    {
        resolved = null;
        if (!_registrations.TryGetValue(serviceType, out var registrations) || registrations.Count <= 0)
        {
            return false;
        }

        var registration = registrations[^1];
        if (registration.Instance != null)
        {
            resolved = registration.Instance;
            return true;
        }

        var instance = CreateOwnedInstance(
            registration.ImplementationType,
            serviceType,
            childRegistrations,
            childScope,
            this);

        if (registration.Singleton)
        {
            registration.Instance = instance;
        }

        resolved = instance;
        return true;
    }

    private bool TryResolveGenericEnumerable(
        Type serviceType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope,
        out object? resolved)
    {
        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            resolved = null;
            return false;
        }

        var elementType = serviceType.GetGenericArguments()[0];
        resolved = ResolveEnumerable(elementType, childRegistrations, childScope);
        return true;
    }

    private bool TryApplyInterceptors(
        Type serviceType,
        Type? requestingType,
        ScopedContainer? childScope,
        out object? resolved)
    {
        resolved = null;
        IServiceResolver interceptorResolver = childScope ?? this;
        foreach (var interceptor in _interceptors)
        {
            if (interceptor.TryResolve(serviceType, requestingType, interceptorResolver, out var overrideResult))
            {
                resolved = overrideResult;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveFromChildScope(
        Type serviceType,
        Type? requestingType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope,
        out object? resolved)
    {
        resolved = null;
        if (childRegistrations == null ||
            !childRegistrations.TryGetValue(serviceType, out var foundInChild) ||
            foundInChild.Count <= 0)
        {
            return false;
        }

        var registration = foundInChild[^1];
        resolved = registration switch
        {
            { Instance: not null } => registration.Instance,

            { Singleton: true } when childScope != null => childScope.ResolveCore(
                serviceType,
                requestingType,
                null,
                null),

            _ => CreateOwnedInstance(
                registration.ImplementationType,
                serviceType,
                childRegistrations,
                childScope,
                childScope)
        };

        return (resolved is not null);
    }

    private object CreateOwnedInstance(
        Type implementationType,
        Type serviceType,
        Dictionary<Type, List<Registration>>? childRegistrations,
        ScopedContainer? childScope,
        ScopedContainer? owner)
    {
        var obj = CreateInstance(implementationType, serviceType, childRegistrations, childScope);
        owner?.TrackInstanceForDisposal(obj);
        return obj;
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

        var effectiveOverrides = MergeRegistrations(_registrations, extraRegistrations);

        if (_parent != null)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var parentEnumerable = _parent.ResolveCore(enumerableType, null, effectiveOverrides, childScope);
            if (parentEnumerable is IEnumerable enumerable)
            {
                instances.AddRange(enumerable.Cast<object>());
            }
        }

        if (_registrations.TryGetValue(elementType, out var registrations))
        {
            foreach (var registration in registrations)
            {
                if (registration.Instance != null)
                {
                    instances.Add(registration.Instance);
                    continue;
                }

                var instance = CreateInstance(
                    registration.ImplementationType,
                    elementType,
                    effectiveOverrides,
                    childScope);

                if (registration.Singleton)
                {
                    registration.Instance = instance;
                }

                instances.Add(instance);
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
        ScopedContainer? owningScope = null)
    {
        var (constructor, parameters) = GetConstructorAndParams(implementationType);
        var parameterInstances = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            parameterInstances[i] = ResolveCore(
                parameters[i].ParameterType,
                implementationType,
                extraRegistrations,
                owningScope);
        }

        var instance = Activator.CreateInstance(implementationType, parameterInstances);

        return instance ??
            throw new InvalidOperationException($"Failed to create instance of type {implementationType}.");
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
