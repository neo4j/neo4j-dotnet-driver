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

    private readonly ConcurrentStack<IAsyncDisposable> _disposables = new();
    private readonly List<IResolutionInterceptor> _interceptors = [];
    private readonly ScopedContainer? _parent;
    private readonly Dictionary<Type, List<Registration>> _registrations = new();
    private readonly ThreadLocal<Stack<Type>> _resolutionStack = new(() => new Stack<Type>());
    private readonly object _singletonLock = new();
    private bool _disposed;

    private Stack<Type> ResolutionStack => _resolutionStack.Value!;

    private IEnumerable<Type> GetStackOldestFirst()
    {
        return ResolutionStack.Reverse();
    }

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

    public TService Resolve<TService>() => (TService)ResolveCore(typeof(TService), null);

    public object Resolve(Type serviceType) => ResolveCore(serviceType, null);

    public object Resolve(Type serviceType, Type? requestingType) => ResolveCore(serviceType, requestingType);

    public bool TryResolve<T>([NotNullWhen(true)] out T? value)
    {
        var success = TryResolve(typeof(T), out var service);
        value = (T?)service;
        return success;
    }

    public bool TryResolve(Type serviceType, [NotNullWhen(true)] out object? service)
    {
        return TryResolveCore(serviceType, null, out service);
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

    private object ResolveCore(Type serviceType, Type? requestingType)
    {
        if (TryResolveCore(serviceType, requestingType, out var resolved))
        {
            return resolved;
        }

        throw new InvalidOperationException(
            $"Service of type {serviceType} required by {requestingType} is not registered. " +
            $"Resolution chain: {GetResolutionStackString()}");
    }

    private bool TryResolveCore(
        Type serviceType,
        Type? requestingType,
        [NotNullWhen(true)] out object? resolved)
    {
        resolved = null;
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ResolutionStack.Contains(serviceType))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {serviceType}. " +
                $"Resolution chain: {string.Join(" -> ", GetStackOldestFirst().Append(serviceType).Select(t => t.Name))}");
        }

        resolved =
            // interceptors get first crack
            ResolveFromInterceptors(serviceType, requestingType) ??

            // next look in the registrations
            ResolveFromRegistrations(serviceType) ??

            // is it a request for IEnumerable<T> that we can create?
            CreateGenericEnumerable(serviceType);

        return resolved != null;
    }

    private object? ResolveFromRegistrations(Type serviceType)
    {
        var registrations = FindRegistrations(serviceType);
        if (registrations.Count == 0)
        {
            return null;
        }

        var (registration, owner) = registrations[^1];
        return Instantiate(registration, owner, serviceType);
    }

    private Array? CreateGenericEnumerable(Type serviceType)
    {
        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            return null;
        }

        var elementType = serviceType.GetGenericArguments()[0];
        var registrations = FindRegistrations(elementType);
        var elements = Array.CreateInstance(elementType, registrations.Count);
        for (var i = 0; i < registrations.Count; i++)
        {
            var (registration, owner) = registrations[i];
            elements.SetValue(Instantiate(registration, owner, elementType), i);
        }

        return elements;
    }

    private object? ResolveFromInterceptors(Type serviceType, Type? requestingType)
    {
        if (!HasAnyInterceptor())
        {
            return null;
        }

        var resolutionStack = ResolutionStack;
        resolutionStack.Push(serviceType);
        try
        {
            for (var scope = this; scope != null; scope = scope._parent)
            {
                foreach (var interceptor in scope._interceptors)
                {
                    if (interceptor.TryResolve(serviceType, requestingType, this, out var overrideResult))
                    {
                        return overrideResult;
                    }
                }
            }
        }
        finally
        {
            resolutionStack.Pop();
        }

        return null;
    }

    private bool HasAnyInterceptor()
    {
        for (var scope = this; scope != null; scope = scope._parent)
        {
            if (scope._interceptors.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private List<(Registration Registration, ScopedContainer Owner)> FindRegistrations(Type serviceType)
    {
        var found = new List<(Registration, ScopedContainer)>();
        CollectRegistrations(serviceType, found);
        return found;
    }

    private void CollectRegistrations(Type serviceType, List<(Registration, ScopedContainer)> found)
    {
        _parent?.CollectRegistrations(serviceType, found);

        if (!_registrations.TryGetValue(serviceType, out var registrations))
        {
            return;
        }

        foreach (var registration in registrations)
        {
            found.Add((registration, this));
        }
    }

    private object Instantiate(Registration registration, ScopedContainer owner, Type serviceType)
    {
        if (registration.Instance != null)
        {
            return registration.Instance;
        }

        if (registration.Singleton)
        {
            return GetOrCreateSingleton(registration, owner, serviceType);
        }

        return CreateOwnedInstance(registration.ImplementationType, serviceType, owner);
    }

    private object CreateOwnedInstance(Type implementationType, Type serviceType, ScopedContainer owner)
    {
        var obj = CreateInstance(implementationType, serviceType);
        owner.TrackInstanceForDisposal(obj);
        return obj;
    }

    private object GetOrCreateSingleton(Registration registration, ScopedContainer owner, Type serviceType)
    {
        var existing = registration.Instance;
        if (existing != null)
        {
            return existing;
        }

        lock (owner._singletonLock)
        {
            existing = registration.Instance;
            if (existing != null)
            {
                return existing;
            }

            var created = CreateOwnedInstance(registration.ImplementationType, serviceType, owner);
            registration.Instance = created;
            return created;
        }
    }

    private string GetResolutionStackString()
    {
        return string.Join(" -> ", GetStackOldestFirst().Select(t => t.Name));
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

    private object CreateInstance(Type implementationType, Type serviceType)
    {
        var (constructor, parameters) = GetConstructorAndParams(implementationType);
        var constructorArguments = new object[parameters.Length];

        var resolutionStack = ResolutionStack;
        resolutionStack.Push(serviceType);
        try
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                constructorArguments[i] = ResolveCore(parameters[i].ParameterType, implementationType);
            }
        }
        finally
        {
            resolutionStack.Pop();
        }

        var instance = constructor.Invoke(constructorArguments);

        return instance ??
            throw new InvalidOperationException($"Failed to create instance of type {implementationType}.");
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
