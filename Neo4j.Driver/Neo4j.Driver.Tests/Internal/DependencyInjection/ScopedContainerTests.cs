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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.DependencyInjection;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.DependencyInjection;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class ScopedContainerTests
{
    public interface ITestService
    {
    } 

    public interface IDependency
    {
    }

    public interface IMultiImplementer 
    {
    }

    private class TestService(IDependency dependency) : ITestService
    {
        public IDependency Dependency { get; } = dependency;
    }

    private class ServiceWithNoDependencies : ITestService
    {
    }

    private class MultiImplementer1 : IMultiImplementer
    {
    }

    private class MultiImplementer2 : IMultiImplementer
    {
    }

    private class ServiceWithMultipleConstructors
    {
        public int ConstructorCalled { get; }
        public ServiceWithMultipleConstructors() => ConstructorCalled = 0;
        public ServiceWithMultipleConstructors(IDependency dependency) => ConstructorCalled = 1;
        public ServiceWithMultipleConstructors(IDependency dep1, ITestService dep2) => ConstructorCalled = 2;
    }

    public class CircularA
    {
        public CircularA(CircularB b)
        {
        }
    }

    public class CircularB
    {
        public CircularB(CircularA a)
        {
        }
    }

    public class SiblingA(ITestService testService)
    {
        public ITestService InnerTestService { get; } = testService;
    }

    public class SiblingB(ITestService testService)
    {
        public ITestService InnerTestService { get; } = testService;
    }

    public class SiblingParent(SiblingA a, SiblingB b)
    {
        public SiblingA A { get; } = a;
        public SiblingB B { get; } = b;
    }

    private class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private class AsyncDisposableService : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class OrderedDisposable(List<string> order, string name) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            order.Add(name);
            return ValueTask.CompletedTask;
        }
    }

    private class CountingDisposableService : ITestService, IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    [Fact]
    public void Resolve_RegisteredInstanceWithoutOwnership_IsNotDisposed()
    {
        // Hypothesis (H1): the caller retains ownership of a RegisterInstance value unless
        // transferOwnership is set, so resolving it must NOT cause the scope to dispose it.
        var container = new ScopedContainer();
        var instance = new DisposableService();

        container.RegisterInstance(instance);
        _ = container.Resolve<DisposableService>();
        container.Dispose();

        instance.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Resolve_Singleton_DisposedExactlyOnceRegardlessOfResolveCount()
    {
        // Hypothesis (H2): a singleton is one owned instance, so it must be disposed exactly
        // once no matter how many times it is resolved.
        var container = new ScopedContainer();
        container.RegisterType<ITestService, CountingDisposableService>(singleton: true);

        var first = (CountingDisposableService)container.Resolve<ITestService>();
        _ = container.Resolve<ITestService>();
        _ = container.Resolve<ITestService>();
        container.Dispose();

        first.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task ChildScope_ParentOwnedService_IsDisposedByParentNotByChild()
    {
        // Issue 3: a service registered in the parent is owned by the parent even when it is
        // first resolved through a child scope. Disposing the child must not dispose it, and
        // the parent must dispose it exactly once (no cross-scope double-tracking).
        var parent = new ScopedContainer();
        parent.RegisterType<ITestService, CountingDisposableService>();

        var child = parent.CreateChildScope(_ => { });
        var resolved = (CountingDisposableService)child.Resolve<ITestService>();

        await child.DisposeAsync();
        resolved.DisposeCount.Should().Be(0);

        await parent.DisposeAsync();
        resolved.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task Resolve_SelfRegisteredResolutionScope_IsNotEnrolledForDisposal()
    {
        // Issue 4: the container registers itself as IResolutionScope — a registered instance
        // under an interface. Resolving such an instance must return it as-is without enrolling
        // it for disposal (the create path is the only thing that transfers ownership). Mirrored
        // here with an IAsyncDisposable registered instance resolved via its interface.
        var container = new ScopedContainer();

        container.Resolve<IResolutionScope>().Should().BeSameAs(container);

        var instance = new AsyncDisposableService();
        container.RegisterInstance<IAsyncDisposable>(instance);
        _ = container.Resolve<IAsyncDisposable>();
        await container.DisposeAsync();

        instance.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void RegisterInstance_ResolvesRegisteredInstance()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var instance = mocker.Get<ITestService>();

        container.RegisterInstance(instance);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeSameAs(instance);
    }

    [Fact]
    public void RegisterType_CreatesNewInstanceWithConstructorResolution()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var dependency = mocker.Get<IDependency>();

        container.RegisterInstance(dependency);
        container.RegisterType<ITestService, TestService>();

        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeOfType<TestService>();
        ((TestService)resolved).Dependency.Should().BeSameAs(dependency);
    }

    [Fact]
    public void RegisterType_WithNoConstructorDependencies_CreatesInstance()
    {
        var container = new ScopedContainer();

        container.RegisterType<ITestService, ServiceWithNoDependencies>();
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeOfType<ServiceWithNoDependencies>();
    }

    [Fact]
    public void RegisterType_SelectsConstructorWithMostParameters()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();

        container.RegisterInstance(mocker.Get<IDependency>());
        container.RegisterInstance(mocker.Get<ITestService>());
        container.RegisterType<ServiceWithMultipleConstructors>();

        var resolved = container.Resolve<ServiceWithMultipleConstructors>();

        resolved.ConstructorCalled.Should().Be(2);
    }

    [Fact]
    public void Resolve_ThrowsForUnregisteredService()
    {
        var container = new ScopedContainer();

        var act = () => container.Resolve<ITestService>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public void Resolve_ThrowsForCircularDependency()
    {
        var container = new ScopedContainer();
        container.RegisterType<CircularA>();
        container.RegisterType<CircularB>();

        var act = () => container.Resolve<CircularA>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Circular dependency*")
            .WithMessage("*CircularA -> CircularB -> CircularA*");
    }

    [Fact]
    public void Resolve_SucceedsForMultipleDependentsOnSameService()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();

        var testService = mocker.Get<ITestService>();

        container.RegisterInstance(testService);
        container.RegisterType<SiblingA>();
        container.RegisterType<SiblingB>();
        container.RegisterType<SiblingParent>();

        var parent = container.Resolve<SiblingParent>();
        parent.Should().NotBeNull();
        parent.A.InnerTestService.Should().BeSameAs(testService);
        parent.B.InnerTestService.Should().BeSameAs(testService);
    }

    [Fact]
    public void ResolveEnumerable_ResolvesAllImplementers()
    {
        var container = new ScopedContainer();
        container.RegisterType<IMultiImplementer, MultiImplementer1>();
        container.RegisterType<IMultiImplementer, MultiImplementer2>();

        var resolved = container.Resolve<IEnumerable<IMultiImplementer>>().ToArray();

        resolved.Should().HaveCount(2);
        resolved.Should().ContainSingle(x => x is MultiImplementer1);
        resolved.Should().ContainSingle(x => x is MultiImplementer2);
    }

    [Fact]
    public void ResolveEnumerable_ReturnsEmptyForNoImplementers()
    {
        var container = new ScopedContainer();

        var resolved = container.Resolve<IEnumerable<IMultiImplementer>>();

        resolved.Should().BeEmpty();
    }

    [Fact]
    public void RegisterInterceptor_CallsInterceptorDuringResolution()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var mockInterceptor = mocker.GetMock<IResolutionInterceptor>();
        var overrideInstance = mocker.Get<ITestService>();

        mockInterceptor
            .Setup(x => x.TryResolve(
                typeof(ITestService),
                It.IsAny<Type>(),
                It.IsAny<IServiceResolver>(),
                out It.Ref<object>.IsAny))
            .Returns(
                new TryResolveDelegate((_, _, _, out service) =>
                {
                    service = overrideInstance;
                    return true;
                }));

        container.RegisterInterceptor(mockInterceptor.Object);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeSameAs(overrideInstance);
    }

    private delegate bool TryResolveDelegate(
        Type serviceType,
        Type requestingType,
        IServiceResolver resolver,
        out object service);

    [Fact]
    public void RegisterInterceptor_FallsBackToNormalResolutionWhenInterceptorReturnsFalse()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var mockInterceptor = mocker.GetMock<IResolutionInterceptor>();
        var instance = mocker.Get<ITestService>();

        mockInterceptor
            .Setup(x => x.TryResolve(
                It.IsAny<Type>(),
                It.IsAny<Type>(),
                It.IsAny<IServiceResolver>(),
                out It.Ref<object>.IsAny))
            .Returns(false);

        container.RegisterInterceptor(mockInterceptor.Object);
        container.RegisterInstance(instance);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeSameAs(instance);
    }

    [Fact]
    public void CreateScope_CreatesChildContainerWithParentDelegation()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var parentInstance = mocker.Get<ITestService>();

        container.RegisterInstance(parentInstance);
        var scope = container.CreateChildScope(_ => {});

        var resolved = scope.Resolve<ITestService>();

        resolved.Should().BeSameAs(parentInstance);
    }

    [Fact]
    public void CreateScope_ChildRegistrationsDoNotAffectParent()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var childInstance = mocker.Get<ITestService>();

        var scope = container.CreateChildScope(x => x.RegisterInstance(childInstance));

        var act = () => container.Resolve<ITestService>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateScope_ChildOverridesParentRegistration()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var parentInstance = mocker.Get<ITestService>();
        var childInstance = new Mock<ITestService>().Object;

        container.RegisterInstance(parentInstance);
        var scope = (ScopedContainer)container.CreateChildScope(x => x.RegisterInstance(childInstance));

        var resolved = scope.Resolve<ITestService>();

        resolved.Should().BeSameAs(childInstance);
    }

    [Fact]
    public void ResolveEnumerable_IncludesParentAndChildImplementers()
    {
        var container = new ScopedContainer();
        container.RegisterType<IMultiImplementer, MultiImplementer1>();

        var scope = (ScopedContainer)container.CreateChildScope(x =>
            x.RegisterType<IMultiImplementer, MultiImplementer2>());

        var resolved = scope.Resolve<IEnumerable<IMultiImplementer>>().ToArray();

        resolved.Should().HaveCount(2);
        resolved.Should().ContainSingle(x => x is MultiImplementer1);
        resolved.Should().ContainSingle(x => x is MultiImplementer2);
    }

    [Fact]
    public void Dispose_DisposesCreatedDisposableInstances()
    {
        var container = new ScopedContainer();
        container.RegisterType<DisposableService>();

        var resolved = container.Resolve<DisposableService>();
        container.Dispose();

        resolved.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DoesNotDisposeRegisteredInstances()
    {
        // Default: caller retains ownership; scope does not dispose.
        var container = new ScopedContainer();
        var instance = new DisposableService();

        container.RegisterInstance(instance);
        container.Dispose();

        instance.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_DisposesOwnedRegisteredInstances()
    {
        var container = new ScopedContainer();
        var instance = new AsyncDisposableService();

        container.RegisterInstance(instance, transferOwnership: true);
        await container.DisposeAsync();

        instance.IsDisposed.Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(ITestService))]
    [InlineData(typeof(IDependency))]
    public void Resolve_ThrowsAfterDispose(Type serviceType)
    {
        var container = new ScopedContainer();
        container.Dispose();

        var act = () => container.Resolve(serviceType);

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CreateScope_ThrowsAfterDispose()
    {
        var container = new ScopedContainer();
        container.Dispose();

        var act = () => container.CreateChildScope(_ => {});

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RegisterType_WithTypeWithNoPublicConstructor_ThrowsOnResolve()
    {
        var container = new ScopedContainer();
        container.RegisterType<PrivateConstructorService>();

        var act = () => container.Resolve<PrivateConstructorService>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*no public constructors*");
    }

    private class PrivateConstructorService
    {
        private PrivateConstructorService()
        {
        }
    }

    // -------------------------------------------------------------------------
    // Child-scope override flow-through tests
    //
    // These test the new behaviour where child-scope registrations are passed as
    // overrides into the parent's resolution chain, so that a service registered
    // only in the parent scope can still be fully instantiated using dependencies
    // that live in the child scope.
    // -------------------------------------------------------------------------

    // Test types for override flow-through scenarios

    public interface IOverrideA { }
    public interface IOverrideB { }
    public interface IOverrideC { }

    // A depends on B — one level deep
    private class ServiceA_NeedsB(IOverrideB b) : IOverrideA
    {
        public IOverrideB B { get; } = b;
    }

    // A depends on B and C — two siblings
    private class ServiceA_NeedsBC(IOverrideB b, IOverrideC c) : IOverrideA
    {
        public IOverrideB B { get; } = b;
        public IOverrideC C { get; } = c;
    }

    // B depends on C — for transitive chain A→B→C
    private class ServiceB_NeedsC(IOverrideC c) : IOverrideB
    {
        public IOverrideC C { get; } = c;
    }

    private class ServiceB_NoDeps : IOverrideB { }
    private class ServiceC_NoDeps : IOverrideC { }

    private class DisposableServiceB : IOverrideB, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void ChildScope_CanResolveParentServiceUsingChildInstanceDep()
    {
        // Parent knows how to build IOverrideA (needs IOverrideB).
        // Child supplies IOverrideB.
        // Resolving IOverrideA from the child should succeed and use the child's IOverrideB.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childB));

        var result = child.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsB>();
        ((ServiceA_NeedsB)result).B.Should().BeSameAs(childB);
    }

    [Fact]
    public void ChildScope_CanResolveParentServiceUsingMultipleChildDeps()
    {
        // Parent knows how to build IOverrideA (needs IOverrideB and IOverrideC).
        // Child supplies both.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsBC>();

        var childB = new Mock<IOverrideB>().Object;
        var childC = new Mock<IOverrideC>().Object;
        var child = parent.CreateChildScope(r =>
        {
            r.RegisterInstance(childB);
            r.RegisterInstance(childC);
        });

        var result = child.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsBC>();
        ((ServiceA_NeedsBC)result).B.Should().BeSameAs(childB);
        ((ServiceA_NeedsBC)result).C.Should().BeSameAs(childC);
    }

    [Fact]
    public void ChildScope_ChildDepOverridesParentDepDuringParentInstantiation()
    {
        // Parent has IOverrideA → ServiceA_NeedsB, and IOverrideB → ServiceB_NoDeps.
        // Child overrides IOverrideB with its own instance.
        // Resolving IOverrideA from the child should use the child's IOverrideB, not
        // the parent's ServiceB_NoDeps.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();
        parent.RegisterType<IOverrideB, ServiceB_NoDeps>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childB));

        var result = child.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsB>();
        ((ServiceA_NeedsB)result).B.Should().BeSameAs(childB);
    }

    [Fact]
    public void ChildScope_TransitiveChildDepVisibleThroughParentChain()
    {
        // Parent has IOverrideA → ServiceA_NeedsB, IOverrideB → ServiceB_NeedsC.
        // Child supplies IOverrideC.
        // Resolving IOverrideA from child: parent creates ServiceA_NeedsB,
        // which needs IOverrideB; parent creates ServiceB_NeedsC,
        // which needs IOverrideC — found in child.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();
        parent.RegisterType<IOverrideB, ServiceB_NeedsC>();

        var childC = new Mock<IOverrideC>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childC));

        var result = child.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsB>();
        var innerB = (ServiceB_NeedsC)((ServiceA_NeedsB)result).B;
        innerB.C.Should().BeSameAs(childC);
    }

    [Fact]
    public void ChildScope_ParentDepsNotInChildStillResolveFromParent()
    {
        // Child has nothing extra. Parent has everything needed.
        // Should still work — no overrides required.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();
        parent.RegisterType<IOverrideB, ServiceB_NoDeps>();

        var child = parent.CreateChildScope(_ => { });

        var result = child.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsB>();
        ((ServiceA_NeedsB)result).B.Should().BeOfType<ServiceB_NoDeps>();
    }

    [Fact]
    public void ChildScope_ServiceInChildNotParent_ResolvesDirectlyFromChild()
    {
        // Child registers IOverrideA directly — no parent lookup needed.
        var parent = new ScopedContainer();
        var childA = new Mock<IOverrideA>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childA));

        var result = child.Resolve<IOverrideA>();

        result.Should().BeSameAs(childA);
    }

    [Fact]
    public void GrandchildScope_CanResolveParentServiceUsingDepsFromBothIntermediateScopeAndOwnScope()
    {
        // Parent knows how to build IOverrideA (needs IB and IC).
        // Child scope supplies IB.
        // Grandchild scope supplies IC.
        // Resolving IOverrideA from the grandchild must use IB from child AND IC from grandchild.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsBC>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childB));

        var childC = new Mock<IOverrideC>().Object;
        var grandchild = child.CreateChildScope(r => r.RegisterInstance(childC));

        var result = grandchild.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsBC>();
        ((ServiceA_NeedsBC)result).B.Should().BeSameAs(childB);
        ((ServiceA_NeedsBC)result).C.Should().BeSameAs(childC);
    }

    [Fact]
    public void GrandchildScope_OwnRegistrationWinsOverIntermediateScopeForSameInterface()
    {
        // Grandchild overrides IB that the child also provides — grandchild should win.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childB));

        var grandchildB = new Mock<IOverrideB>().Object;
        var grandchild = child.CreateChildScope(r => r.RegisterInstance(grandchildB));

        var result = grandchild.Resolve<IOverrideA>();

        result.Should().BeOfType<ServiceA_NeedsB>();
        ((ServiceA_NeedsB)result).B.Should().BeSameAs(grandchildB);
    }

    [Fact]
    public void ChildScope_ResolveEnumerable_UsesChildDepsWhenInstantiatingParentRegistrations()
    {
        // Parent has two IOverrideA implementations, both needing IOverrideB.
        // Child supplies IOverrideB.
        // Asking child for IEnumerable<IOverrideA> should return both instances,
        // each constructed with the child's IOverrideB.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childB));

        var results = child.Resolve<IEnumerable<IOverrideA>>().ToArray();

        results.Should().HaveCount(1);
        results[0].Should().BeOfType<ServiceA_NeedsB>();
        ((ServiceA_NeedsB)results[0]).B.Should().BeSameAs(childB);
    }

    [Fact]
    public async Task ChildScope_DisposesChildRegisteredDependencyUsedByParentService()
    {
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();

        var child = parent.CreateChildScope(r => r.RegisterType<IOverrideB, DisposableServiceB>());

        var result = (ServiceA_NeedsB)child.Resolve<IOverrideA>();
        var childB = (DisposableServiceB)result.B;

        await child.DisposeAsync();

        childB.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DisposesAsyncDisposableInstances()
    {
        var container = new ScopedContainer();
        container.RegisterType<AsyncDisposableService>();

        var resolved = container.Resolve<AsyncDisposableService>();
        await container.DisposeAsync();

        resolved.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DisposesChildScopeWhenParentIsDisposed()
    {
        var container = new ScopedContainer();

        var child = container.CreateChildScope(r => r.RegisterType<AsyncDisposableService>());
        var childResolved = child.Resolve<AsyncDisposableService>();

        await container.DisposeAsync();

        childResolved!.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DisposesInReverseCreationOrder()
    {
        var order = new List<string>();
        var container = new ScopedContainer();

        // Owned instances added to _disposables in creation order
        container.RegisterInstance(new OrderedDisposable(order, "first"), transferOwnership: true);
        container.RegisterInstance(new OrderedDisposable(order, "second"), transferOwnership: true);

        // Child scope created last — appended to parent _disposables after both parent instances
        container.CreateChildScope(r =>
            r.RegisterInstance(new OrderedDisposable(order, "child"), transferOwnership: true));

        await container.DisposeAsync();

        // Reverse creation order: child (last added) disposed first
        order.Should().Equal("child", "second", "first");
    }

    [Fact]
    public void ChildScope_ResolveEnumerable_IncludesParentAndChildImplementations()
    {
        // Parent has one IOverrideA implementation; child adds another.
        // Both require IOverrideB which the child supplies.
        var parent = new ScopedContainer();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r =>
        {
            r.RegisterInstance(childB);
            r.RegisterType<IOverrideA, ServiceA_NeedsBC>(); // child adds a second impl (also needs IOverrideB)
            r.RegisterInstance(new Mock<IOverrideC>().Object); // satisfy ServiceA_NeedsBC's IOverrideC dep
        });

        var results = child.Resolve<IEnumerable<IOverrideA>>().ToArray();

        results.Should().HaveCount(2);
        results.Should().ContainSingle(x => x is ServiceA_NeedsB);
        results.Should().ContainSingle(x => x is ServiceA_NeedsBC);
        results.OfType<ServiceA_NeedsB>().Single().B.Should().BeSameAs(childB);
        results.OfType<ServiceA_NeedsBC>().Single().B.Should().BeSameAs(childB);
    }

    [Fact]
    public void RegisterType_Singleton_ReturnsSameInstanceForEveryResolve()
    {
        var container = new ScopedContainer();
        container.RegisterType<ITestService, ServiceWithNoDependencies>(singleton: true);

        var first = container.Resolve<ITestService>();
        var second = container.Resolve<ITestService>();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void RegisterType_NonSingleton_ReturnsNewInstanceForEveryResolve()
    {
        var container = new ScopedContainer();
        container.RegisterType<ITestService, ServiceWithNoDependencies>();

        var first = container.Resolve<ITestService>();
        var second = container.Resolve<ITestService>();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void RegisterType_Singleton_IsSharedAcrossChildScopes()
    {
        // A singleton is cached on the registration it was registered against, so a
        // parent-registered singleton is the same instance for every child scope.
        var parent = new ScopedContainer();
        parent.RegisterType<ITestService, ServiceWithNoDependencies>(singleton: true);

        var child1 = parent.CreateChildScope(_ => { });
        var child2 = parent.CreateChildScope(_ => { });

        var fromChild1 = child1.Resolve<ITestService>();
        var fromChild2 = child2.Resolve<ITestService>();

        fromChild1.Should().BeSameAs(fromChild2);
    }

    [Fact]
    public void ResolveEnumerable_Singleton_ReturnsSameInstanceForEveryResolve()
    {
        var container = new ScopedContainer();
        container.RegisterType<IMultiImplementer, MultiImplementer1>(singleton: true);

        var first = container.Resolve<IEnumerable<IMultiImplementer>>().Single();
        var second = container.Resolve<IEnumerable<IMultiImplementer>>().Single();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void ChildSingleton_ShadowsParentForSingleResolve_ButBothAppearInEnumerable()
    {
        // Parent and child each register their own singleton implementation of the same
        // interface. They are independent singletons: the child's shadows the parent's for
        // single-service resolution, while an enumerable resolve returns both — and the
        // enumerable yields the same cached instances as the single resolves.
        var parent = new ScopedContainer();
        parent.RegisterType<IMultiImplementer, MultiImplementer1>(singleton: true);
        var child = parent.CreateChildScope(r => r.RegisterType<IMultiImplementer, MultiImplementer2>(singleton: true));

        var fromChild = child.Resolve<IMultiImplementer>();
        var fromParent = parent.Resolve<IMultiImplementer>();
        var childEnumerable = child.Resolve<IEnumerable<IMultiImplementer>>().ToArray();

        fromChild.Should().BeOfType<MultiImplementer2>();
        fromParent.Should().BeOfType<MultiImplementer1>();
        fromChild.Should().NotBeSameAs(fromParent);

        childEnumerable.Should().HaveCount(2);
        childEnumerable.Should().Contain(x => ReferenceEquals(x, fromParent));
        childEnumerable.Should().Contain(x => ReferenceEquals(x, fromChild));
    }

    [Fact]
    public void RegisterType_ImplementationNotAssignableToService_Throws()
    {
        var container = new ScopedContainer();

        var act = () => container.RegisterType(typeof(ITestService), typeof(MultiImplementer1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterInstance_NullInstance_ThrowsArgumentNullException()
    {
        var container = new ScopedContainer();

        var act = () => container.RegisterInstance<ITestService>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterInterceptor_NullInterceptor_ThrowsArgumentNullException()
    {
        var container = new ScopedContainer();

        var act = () => container.RegisterInterceptor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterInstance_AfterDispose_Throws()
    {
        var container = new ScopedContainer();
        var instance = new Mock<ITestService>().Object;
        container.Dispose();

        var act = () => container.RegisterInstance(instance);

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RegisterType_AfterDispose_Throws()
    {
        var container = new ScopedContainer();
        container.Dispose();

        var act = () => container.RegisterType<ITestService, ServiceWithNoDependencies>();

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RegisterInterceptor_AfterDispose_Throws()
    {
        var container = new ScopedContainer();
        var interceptor = new Mock<IResolutionInterceptor>().Object;
        container.Dispose();

        var act = () => container.RegisterInterceptor(interceptor);

        act.Should().Throw<ObjectDisposedException>();
    }
}
