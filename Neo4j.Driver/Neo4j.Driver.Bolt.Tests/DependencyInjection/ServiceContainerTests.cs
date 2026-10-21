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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Bolt.DependencyInjection;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.DependencyInjection;

[TestFixture]
internal class ServiceContainerTests
{
    [Test]
    public void Resolve_RegisteredInterface_ReturnsImplementation()
    {
        var container = new ServiceContainer().Register<IWidget, Widget>();
        var widget = container.Resolve<IWidget>();
        widget.Should().BeOfType<Widget>();
    }

    [Test]
    public void Resolve_ConcreteWithDependencies_BuildsGraph()
    {
        var container = new ServiceContainer()
            .Register<IWidget, Widget>()
            .Register<IGadget, Gadget>()
            .Register<Root, Root>();

        var root = container.Resolve<Root>();
        root.Widget.Should().BeOfType<Widget>();
        root.Gadget.Should().BeOfType<Gadget>();
    }

    [Test]
    public void Resolve_RegisterInstance_ReturnsSameReference()
    {
        var dep = new Dep();
        var container = new ServiceContainer()
            .RegisterInstance<IDep>(dep)
            .Register<Consumer, Consumer>();

        var a = container.Resolve<Consumer>();
        var b = container.Resolve<Consumer>();
        a.Dep.Should().BeSameAs(dep);
        b.Dep.Should().BeSameAs(dep);
        a.Should().NotBeSameAs(b);
    }

    [Test]
    public void Resolve_CircularDependency_ThrowsInvalidOperationException()
    {
        var container = new ServiceContainer()
            .Register<ICircularA, CircularA>()
            .Register<ICircularB, CircularB>();

        var act = () => container.Resolve<ICircularA>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Circular dependency*");
    }

    [Test]
    public void Resolve_TwoConstructorParametersOfSameTransientType_Succeeds()
    {
        var container = new ServiceContainer().Register<IWidget, Widget>();
        var pair = container.Resolve<TwoWidgets>();
        pair.First.Should().BeOfType<Widget>();
        pair.Second.Should().BeOfType<Widget>();
        pair.First.Should().NotBeSameAs(pair.Second);
    }

    [Test]
    public void Resolve_SameTypeAsSecondParameterWhileOuterStillConstructing_Succeeds()
    {
        var container = new ServiceContainer().Register<IWidget, Widget>();
        var root = container.Resolve<RootWithWidgetAndSameInterface>();
        root.Widget.Should().BeOfType<Widget>();
        root.Another.Should().BeOfType<Widget>();
    }

    [Test]
    public void Resolve_UnregisteredInterface_ThrowsInvalidOperationException()
    {
        var container = new ServiceContainer();
        var act = () => container.Resolve<IWidget>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*No registration*");
    }

    [Test]
    public void Resolve_UnparameterizedConcrete_ResolvesWithoutExplicitRegister()
    {
        var container = new ServiceContainer();
        var leaf = container.Resolve<Leaf>();
        leaf.Should().NotBeNull();
    }

    [Test]
    public void CreateScope_ResolvesFromParentWhenNotRegisteredLocally()
    {
        var parent = new ServiceContainer().Register<IWidget, Widget>();
        using var child = parent.CreateScope();
        child.Resolve<IWidget>().Should().BeOfType<Widget>();
    }

    [Test]
    public void CreateScope_LocalRegistrationOverridesParent()
    {
        var parent = new ServiceContainer().Register<IWidget, Widget>();
        using var child = parent.CreateScope();
        child.Register<IWidget, OtherWidget>();
        child.Resolve<IWidget>().Should().BeOfType<OtherWidget>();
    }

    [Test]
    public void CreateScope_NestedChildStillResolvesViaChain()
    {
        var root = new ServiceContainer().Register<IWidget, Widget>();
        using var middle = root.CreateScope();
        using var leaf = middle.CreateScope();
        leaf.Resolve<IWidget>().Should().BeOfType<Widget>();
    }

    [Test]
    public void CreateScope_ConstructorDependenciesResolvedFromParent()
    {
        var dep = new Dep();
        var parent = new ServiceContainer()
            .RegisterInstance<IDep>(dep)
            .Register<Consumer, Consumer>();
        using var child = parent.CreateScope();
        var consumer = child.Resolve<Consumer>();
        consumer.Dep.Should().BeSameAs(dep);
    }

    [Test]
    public void CreateScope_IEnumerableDelegatesToParentWhenLocalListEmpty()
    {
        var parent = new ServiceContainer()
            .Register<IWidget, Widget>()
            .Register<IWidget, OtherWidget>();
        using var child = parent.CreateScope();
        var all = child.Resolve<IEnumerable<IWidget>>().ToList();
        all.Should().HaveCount(2);
        all[0].Should().BeOfType<Widget>();
        all[1].Should().BeOfType<OtherWidget>();
    }

    [Test]
    public void Dispose_ThenResolve_ThrowsObjectDisposedException()
    {
        var parent = new ServiceContainer().Register<IWidget, Widget>();
        var child = parent.CreateScope();
        child.Dispose();
        var act = () => child.Resolve<IWidget>();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void DisposeChild_ParentStillResolves()
    {
        var parent = new ServiceContainer().Register<IWidget, Widget>();
        var child = parent.CreateScope();
        child.Dispose();
        parent.Resolve<IWidget>().Should().BeOfType<Widget>();
    }

    [Test]
    public void Resolve_MultipleRegistrationsForSameService_LastRegistrationWins()
    {
        // Plain Resolve<T> picks the last registered implementation when several exist; use IEnumerable<T> for all.
        var container = new ServiceContainer()
            .Register<IWidget, Widget>()
            .Register<IWidget, OtherWidget>();

        container.Resolve<IWidget>().Should().BeOfType<OtherWidget>();
    }

    [Test]
    public void Resolve_IEnumerable_ReturnsAllImplementationsInRegistrationOrder()
    {
        var container = new ServiceContainer()
            .Register<Root, Root>()
            .Register<IWidget, Widget>()
            .Register<IWidget, OtherWidget>();

        var all = container.Resolve<IEnumerable<IWidget>>().ToList();
        all.Should().HaveCount(2);
        all[0].Should().BeOfType<Widget>();
        all[1].Should().BeOfType<OtherWidget>();
    }

    [Test]
    public void RegisterTypesFromThisAssembly_ResolvesValueDecoderProvider()
    {
        var container = new ServiceContainer()
            .RegisterInstance<ILogger>(Mock.Of<ILogger>())
            .RegisterTypesFromThisAssembly();

        var provider = container.Resolve<ValueDecoderProvider>();
        provider.Should().NotBeNull();
    }

    private interface IWidget
    {
    }

    private class Widget : IWidget;

    private class OtherWidget : IWidget;

    private interface IGadget
    {
    }

    private class Gadget : IGadget;

    private class Root(IWidget widget, IGadget gadget)
    {
        public IWidget Widget { get; } = widget;
        public IGadget Gadget { get; } = gadget;
    }

    private class TwoWidgets(IWidget first, IWidget second)
    {
        public IWidget First { get; } = first;
        public IWidget Second { get; } = second;
    }

    private class RootWithWidgetAndSameInterface(IWidget widget, IWidget another)
    {
        public IWidget Widget { get; } = widget;
        public IWidget Another { get; } = another;
    }

    private interface IDep
    {
    }

    private class Dep : IDep;

    private class Consumer(IDep dep)
    {
        public IDep Dep { get; } = dep;
    }

    private interface ICircularA
    {
    }

    private interface ICircularB
    {
    }

    private class CircularA : ICircularA
    {
        public CircularA(ICircularB b) => _ = b;
    }

    private class CircularB : ICircularB
    {
        public CircularB(ICircularA a) => _ = a;
    }

    private class Leaf;
}
