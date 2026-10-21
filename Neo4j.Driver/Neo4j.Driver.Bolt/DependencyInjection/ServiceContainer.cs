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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Neo4j.Driver.Bolt.Extensions;

namespace Neo4j.Driver.Bolt.DependencyInjection;

/// <summary>
/// Internal-use container: register implementations (several per service), instances, optional assembly scan,
/// and <see cref="Resolve{T}"/> with constructor injection. Several implementations for one service: plain
/// <c>Resolve&lt;T&gt;</c> uses the last registration; <c>IEnumerable&lt;T&gt;</c> returns all in order.
/// Optional <see cref="IServiceResolver"/> parent: when this scope has no local answer, resolution delegates to the parent.
/// </summary>
public class ServiceContainer : IServiceResolver, IDisposable
{
    private readonly IServiceResolver? _parent;
    private readonly Dictionary<Type, HashSet<Type>> _implementations = new();
    private readonly Dictionary<Type, object> _instances = new();
    private bool _disposed;

    /// <summary>
    /// Creates a root container (no parent).
    /// </summary>
    public ServiceContainer()
        : this(parent: null)
    {
    }

    /// <summary>
    /// Creates a scoped container. Registrations on this instance override the parent; anything not
    /// registered locally is resolved via <paramref name="parent"/>.
    /// </summary>
    public ServiceContainer(IServiceResolver? parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Creates a child scope with this container as parent. Disposing the child does not dispose the parent.
    /// </summary>
    public ServiceContainer CreateScope() => new(this);

    /// <summary>
    /// Registers <typeparamref name="TImplementation"/> for <typeparamref name="TService"/>.
    /// Later registrations for the same service override plain <c>Resolve&lt;TService&gt;</c>; use
    /// <c>IEnumerable&lt;TService&gt;</c> for every implementation.
    /// </summary>
    public ServiceContainer Register<TService, TImplementation>()
        where TImplementation : class, TService
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        AddRegistration(typeof(TService), typeof(TImplementation));
        return this;
    }

    public ServiceContainer RegisterInstance<TService>(TService instance)
        where TService : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(instance);
        _instances[typeof(TService)] = instance;
        return this;
    }

    /// <summary>
    /// Registers concrete types in <paramref name="assembly"/> as themselves and for each interface
    /// also defined in that assembly. Types ordered by full name.
    /// </summary>
    public ServiceContainer RegisterTypesFromAssembly(Assembly assembly)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                // Skip non-class, abstract, or generic types
                continue;
            }

            if (type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0)
            {
                // Skip compiler-generated types
                continue;
            }

            AddRegistration(type, type);

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.Assembly == assembly)
                {
                    AddRegistration(interfaceType, type);
                }
            }
        }

        return this;
    }

    public ServiceContainer RegisterTypesFromThisAssembly() =>
        RegisterTypesFromAssembly(typeof(ServiceContainer).Assembly);

    /// <inheritdoc />
    public T Resolve<T>() where T : notnull => (T)Resolve(typeof(T));
    
    public object Resolve(Type serviceType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(serviceType);
        return Resolve(serviceType, new Stack<Type>());
    }

    private object Resolve(Type serviceType, Stack<Type> resolutionStack)
    {
        if (_instances.TryGetValue(serviceType, out var instance))
        {
            // singleton or scoped instance already created
            return instance;
        }

        if (_parent is null && serviceType is { IsClass: true } and { IsAbstract: false })
        {
            // a concrete class resolves to itself by default if nothing registered anywhere up the stack
            return InstantiateService(serviceType, resolutionStack);
        }

        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            // IEnumerable<T>, return all implementations
            return ResolveAll(serviceType, resolutionStack);
        }

        if (_implementations.TryGetValue(serviceType, out var implTypes) && implTypes.Count > 0)
        {
            // last registration wins
            return InstantiateService(implTypes.Last(), resolutionStack); 
        }

        return ResolveFromParentOrThrow(serviceType); // no local registration, delegate to parent if any

    }

    private object ResolveFromParentOrThrow(Type serviceType)
    {
        if (_parent is not null)
        {
            return _parent.Resolve(serviceType);
        }

        throw new InvalidOperationException($"No registration for {serviceType.FullName}.");
    }

    private object ResolveAll(Type serviceType, Stack<Type> resolutionStack)
    {
        var typeofT = serviceType.GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(typeofT);
        var list = (IList)Activator.CreateInstance(listType)!;

        if (!_implementations.TryGetValue(typeofT, out var impls))
        {
            return _parent?.Resolve(serviceType) ?? list;
        }

        foreach (var service in impls.Select(impl => Resolve(impl, resolutionStack)))
        {
            list.Add(service);
        }

        return list;
    }

    private object InstantiateService(Type concreteType, Stack<Type> resolutionStack)
    {
        ThrowIfCircularDependency(concreteType, resolutionStack);

        resolutionStack.Push(concreteType);
        try
        {
            var constructor = SelectConstructor(concreteType);
            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = Resolve(parameters[i].ParameterType, resolutionStack);
            }

            return Activator.CreateInstance(concreteType, args) ??
                throw new InvalidOperationException($"Failed to create instance of {concreteType.FullName}.");
        }
        finally
        {
            resolutionStack.Pop();
        }
    }

    private static void ThrowIfCircularDependency(Type concreteType, Stack<Type> resolutionStack)
    {
        if (resolutionStack.Contains(concreteType))
        {
            var path = string.Join(
                " -> ",
                resolutionStack.Reverse().Append(concreteType).Select(static t => t.FullName ?? t.Name));

            throw new InvalidOperationException(
                $"Circular dependency while resolving {concreteType.FullName}. Path: {path}.");
        }
    }

    private void AddRegistration(Type serviceType, Type implementationType)
    {
        if (_implementations.TryGetValue(serviceType, out var types))
        {
            types.Add(implementationType);
        }
        else
        {
            _implementations[serviceType] = [implementationType];
        }
    }

    private static ConstructorInfo SelectConstructor(Type concreteType)
    {
        var constructors = concreteType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        return constructors.MaxBy(c => c.GetParameters().Length) ??
            throw new InvalidOperationException($"{concreteType.FullName} has no public constructors.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _implementations.Clear();
        _instances.Clear();
    }
}
