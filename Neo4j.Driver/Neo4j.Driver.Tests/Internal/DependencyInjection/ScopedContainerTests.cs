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
            .WithMessage("*Circular dependency*");
    }

    [Fact]
    public void Resolve_SucceedsForMultipleDependentsOnSameService()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();

        var testService = mocker.Get<ITestService>();

        container.RegisterInstance(mocker.Get<IDependency>());
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
    public void RegisterPlugin_CallsPluginDuringResolution()
    {
        var container = new ScopedContainer();
        var mocker = new AutoMocker();
        var mockPlugin = mocker.GetMock<IResolverOverride>();
        var overrideInstance = mocker.Get<ITestService>();

        mockPlugin
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
        var container = new ScopedContainer();
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
        var container = new ScopedContainer();
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
}
