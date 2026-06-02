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
public class ScopedContainerRewriteTests
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

    [Fact]
    public void RegisterInstance_ResolvesRegisteredInstance()
    {
        var container = new ScopedContainerRewrite();
        var mocker = new AutoMocker();
        var instance = mocker.Get<ITestService>();

        container.RegisterInstance(instance);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeSameAs(instance);
    }

    [Fact]
    public void RegisterType_CreatesNewInstanceWithConstructorResolution()
    {
        var container = new ScopedContainerRewrite();
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
        var container = new ScopedContainerRewrite();

        container.RegisterType<ITestService, ServiceWithNoDependencies>();
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeOfType<ServiceWithNoDependencies>();
    }

    [Fact]
    public void RegisterType_SelectsConstructorWithMostParameters()
    {
        var container = new ScopedContainerRewrite();
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
        var container = new ScopedContainerRewrite();

        var act = () => container.Resolve<ITestService>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolve_ThrowsForCircularDependency()
    {
        var container = new ScopedContainerRewrite();
        container.RegisterType<CircularA>();
        container.RegisterType<CircularB>();

        var act = () => container.Resolve<CircularA>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Circular dependency*");
    }

    [Fact]
    public void Resolve_SucceedsForMultipleDependentsOnSameService()
    {
        var container = new ScopedContainerRewrite();
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
        var container = new ScopedContainerRewrite();
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
        var container = new ScopedContainerRewrite();

        var resolved = container.Resolve<IEnumerable<IMultiImplementer>>();

        resolved.Should().BeEmpty();
    }

    [Fact]
    public void RegisterPlugin_CallsPluginDuringResolution()
    {
        var container = new ScopedContainerRewrite();
        var mocker = new AutoMocker();
        var mockPlugin = mocker.GetMock<IResolverOverride>();
        var overrideInstance = mocker.Get<ITestService>();

        mockPlugin
            .Setup(x => x.TryResolve(
                typeof(ITestService),
                It.Is<Type>(t => t == null), // top-level resolve has no requesting type
                It.IsAny<IServiceResolver>(),
                out It.Ref<object>.IsAny))
            .Returns(
                new TryResolveDelegate((_, _, _, out service) =>
                {
                    service = overrideInstance;
                    return true;
                }));

        container.RegisterPlugin(mockPlugin.Object);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeSameAs(overrideInstance);
    }

    private delegate bool TryResolveDelegate(
        Type serviceType,
        Type requestingType,
        IServiceResolver resolver,
        out object service);

    [Fact]
    public void RegisterPlugin_FallsBackToNormalResolutionWhenPluginReturnsFalse()
    {
        var container = new ScopedContainerRewrite();
        var mocker = new AutoMocker();
        var mockPlugin = mocker.GetMock<IResolverOverride>();
        var instance = mocker.Get<ITestService>();

        mockPlugin
            .Setup(x => x.TryResolve(
                It.IsAny<Type>(),
                It.IsAny<Type>(),
                It.IsAny<IServiceResolver>(),
                out It.Ref<object>.IsAny))
            .Returns(false);

        container.RegisterPlugin(mockPlugin.Object);
        container.RegisterInstance(instance);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeSameAs(instance);
    }

    [Fact]
    public void RegisterPlugin_ReceivesRequestingTypeWhenResolvingConstructorDependency()
    {
        var container = new ScopedContainerRewrite();
        var mocker = new AutoMocker();
        var mockPlugin = mocker.GetMock<IResolverOverride>();
        var mockDependency = mocker.Get<IDependency>();

        mockPlugin
            .Setup(x => x.TryResolve(
                typeof(IDependency),
                typeof(TestService), // TestService is the type being constructed
                It.IsAny<IServiceResolver>(),
                out It.Ref<object>.IsAny))
            .Returns(
                new TryResolveDelegate((_, _, _, out service) =>
                {
                    service = mockDependency;
                    return true;
                }));

        container.RegisterType<ITestService, TestService>();
        container.RegisterPlugin(mockPlugin.Object);
        var resolved = container.Resolve<ITestService>();

        resolved.Should().BeOfType<TestService>();
        ((TestService)resolved).Dependency.Should().BeSameAs(mockDependency);
    }

    [Fact]
    public void CreateScope_CreatesChildContainerWithParentDelegation()
    {
        var container = new ScopedContainerRewrite();
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
        var container = new ScopedContainerRewrite();
        var mocker = new AutoMocker();
        var childInstance = mocker.Get<ITestService>();

        var scope = container.CreateChildScope(x => x.RegisterInstance(childInstance));

        var act = () => container.Resolve<ITestService>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateScope_ChildOverridesParentRegistration()
    {
        var container = new ScopedContainerRewrite();
        var mocker = new AutoMocker();
        var parentInstance = mocker.Get<ITestService>();
        var childInstance = new Mock<ITestService>().Object;

        container.RegisterInstance(parentInstance);
        var scope = (ScopedContainerRewrite)container.CreateChildScope(x => x.RegisterInstance(childInstance));

        var resolved = scope.Resolve<ITestService>();

        resolved.Should().BeSameAs(childInstance);
    }

    [Fact]
    public void ResolveEnumerable_IncludesParentAndChildImplementers()
    {
        var container = new ScopedContainerRewrite();
        container.RegisterType<IMultiImplementer, MultiImplementer1>();

        var scope = (ScopedContainerRewrite)container.CreateChildScope(x =>
            x.RegisterType<IMultiImplementer, MultiImplementer2>());

        var resolved = scope.Resolve<IEnumerable<IMultiImplementer>>().ToArray();

        resolved.Should().HaveCount(2);
        resolved.Should().ContainSingle(x => x is MultiImplementer1);
        resolved.Should().ContainSingle(x => x is MultiImplementer2);
    }

    [Fact]
    public void Dispose_DisposesCreatedDisposableInstances()
    {
        var container = new ScopedContainerRewrite();
        container.RegisterType<DisposableService>();

        var resolved = container.Resolve<DisposableService>();
        container.Dispose();

        resolved.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DoesNotDisposeRegisteredInstances()
    {
        var container = new ScopedContainerRewrite();
        var instance = new DisposableService();

        container.RegisterInstance(instance);
        container.Dispose();

        instance.IsDisposed.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(ITestService))]
    [InlineData(typeof(IDependency))]
    public void Resolve_ThrowsAfterDispose(Type serviceType)
    {
        var container = new ScopedContainerRewrite();
        container.Dispose();

        var act = () => container.Resolve(serviceType);

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CreateScope_ThrowsAfterDispose()
    {
        var container = new ScopedContainerRewrite();
        container.Dispose();

        var act = () => container.CreateChildScope(_ => {});

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RegisterType_WithTypeWithNoPublicConstructor_ThrowsOnResolve()
    {
        var container = new ScopedContainerRewrite();
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
    //
    // Known bugs at time of writing (do not fix here, just note):
    //
    //   BUG-1: Resolve(Type, IServiceResolver? extra = null) at line ~71 calls
    //          itself recursively — `return Resolve(serviceType, extra)` binds
    //          back to the same 2-arg overload instead of the 3-arg one.
    //          All tests that fall through to parent will StackOverflow.
    //
    //   BUG-2: The extra-resolver check at line ~83 calls IServiceResolver.Resolve
    //          which throws InvalidOperationException for unregistered services.
    //          The `is { }` guard is never reached for a miss; the exception
    //          propagates instead of falling through to local registrations.
    //
    //   BUG-3: CreateInstance at line ~251 calls Resolve(paramType, requestingType)
    //          without threading extraResolver through, so child-scope overrides
    //          are invisible for transitive dependencies.
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

    [Fact]
    public void ChildScope_CanResolveParentServiceUsingChildInstanceDep()
    {
        // Parent knows how to build IOverrideA (needs IOverrideB).
        // Child supplies IOverrideB.
        // Resolving IOverrideA from the child should succeed and use the child's IOverrideB.
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
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
        // With extraRegistrations ?? _registrations, child's IB is lost when grandchild delegates
        // upward (grandchild's non-null extraRegistrations replaces child's registrations entirely).
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
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
        var parent = new ScopedContainerRewrite();
        parent.RegisterType<IOverrideA, ServiceA_NeedsB>();

        var childB = new Mock<IOverrideB>().Object;
        var child = parent.CreateChildScope(r => r.RegisterInstance(childB));

        var results = child.Resolve<IEnumerable<IOverrideA>>().ToArray();

        results.Should().HaveCount(1);
        results[0].Should().BeOfType<ServiceA_NeedsB>();
        ((ServiceA_NeedsB)results[0]).B.Should().BeSameAs(childB);
    }

    [Fact]
    public void ChildScope_ResolveEnumerable_IncludesParentAndChildImplementations()
    {
        // Parent has one IOverrideA implementation; child adds another.
        // Both require IOverrideB which the child supplies.
        var parent = new ScopedContainerRewrite();
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
}
