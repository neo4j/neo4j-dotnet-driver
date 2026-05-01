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
    private readonly IServiceResolver? _parent;
    private readonly Dictionary<Type, List<Registration>> _registrations = new();
    private readonly ThreadLocal<HashSet<Type>> _resolutionStack = new(() => []);
    private bool _disposed;

    public ScopedContainer() : this(null)
    {
    }

    private ScopedContainer(IServiceResolver? parent)
    {
        _parent = parent;
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
        return (TService)Resolve(typeof(TService), null);
    }

    public object Resolve(Type serviceType)
    {
        return Resolve(serviceType, null);
    }

    public object Resolve(Type serviceType, Type? requestingType)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopedContainer));
        }

        // Check for circular dependencies
        if (_resolutionStack.Value != null && !_resolutionStack.Value.Add(serviceType))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {serviceType}. " +
                $"Resolution chain: {string.Join(" -> ", _resolutionStack.Value.Select(t => t.Name))}");
        }

        try
        {
            // Try resolver overrides first
            foreach (var resolverOverride in _overrides)
            {
                if (resolverOverride.TryResolve(serviceType, requestingType, this, out var overrideResult))
                {
                    return overrideResult;
                }
            }

            // Handle IEnumerable<T> requests
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                return ResolveEnumerable(elementType);
            }

            // Try to resolve from local registrations (most recent wins)
            if (_registrations.TryGetValue(serviceType, out var registrations) && registrations.Count > 0)
            {
                var registration = registrations[^1]; // Most recent registration
                return registration.Instance ?? CreateInstance(registration.ImplementationType, serviceType);
            }

            // Delegate to parent if available
            return _parent != null
                ? _parent.Resolve(serviceType)
                : throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
        }
        finally
        {
            _resolutionStack.Value?.Remove(serviceType);
        }
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
        var serviceType = typeof(TService);
        if (!_registrations.TryGetValue(serviceType, out var registrations))
        {
            registrations = [];
            _registrations[serviceType] = registrations;
        }

        registrations.Add(new Registration(typeof(TImplementation)));
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

    private Array ResolveEnumerable(Type elementType)
    {
        var instances = new List<object>();

        // Collect from parent first
        if (_parent != null)
        {
            try
            {
                var parentEnumerable = _parent.Resolve(typeof(IEnumerable<>).MakeGenericType(elementType));
                if (parentEnumerable is IEnumerable enumerable)
                {
                    instances.AddRange(enumerable.Cast<object>());
                }
            }
            catch (InvalidOperationException)
            {
                // Parent doesn't have any, continue
            }
        }

        // Add local registrations
        foreach (var (type, registrations) in _registrations)
        {
            if (elementType.IsAssignableFrom(type))
            {
                foreach (var registration in registrations)
                {
                    instances.Add(registration.Instance ?? CreateInstance(registration.ImplementationType, type));
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

    private object CreateInstance(Type implementationType, Type requestingType)
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
            parameterInstances[i] = Resolve(parameters[i].ParameterType, requestingType);
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

        return instance;
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
