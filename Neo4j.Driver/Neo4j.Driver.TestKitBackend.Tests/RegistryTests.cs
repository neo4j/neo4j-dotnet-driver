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
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class RegistryTests
{
    private readonly Registry _registry = new();

    [Fact]
    public void Register_returns_a_registry_object_carrying_the_registered_object()
    {
        var stored = new Stored();

        var registered = _registry.Register(stored);

        registered.Object.Should().BeSameAs(stored);
        registered.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Register_assigns_distinct_ids()
    {
        var first = _registry.Register(new Stored());
        var second = _registry.Register(new Stored());

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public void Get_returns_the_registered_object_under_its_id()
    {
        var stored = new Stored();
        var registered = _registry.Register(stored);

        var got = _registry.Get<Stored>(registered.Id);

        got.Object.Should().BeSameAs(stored);
        got.Id.Should().Be(registered.Id);
    }

    [Fact]
    public void Get_throws_for_an_unknown_id()
    {
        var get = () => _registry.Get<Stored>("no-such-id");

        get.Should().Throw<TestKitProtocolException>().WithMessage("*no-such-id*");
    }

    [Fact]
    public void Get_throws_when_the_id_belongs_to_an_object_of_a_different_type()
    {
        var registered = _registry.Register(new Stored());

        var get = () => _registry.Get<OtherStored>(registered.Id);

        get.Should().Throw<TestKitProtocolException>().WithMessage($"*{registered.Id}*");
    }

    [Fact]
    public void Remove_makes_the_id_unknown()
    {
        var registered = _registry.Register(new Stored());

        _registry.Remove(registered.Id);

        var get = () => _registry.Get<Stored>(registered.Id);
        get.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public async Task DisposeAsync_disposes_every_registered_disposable_object()
    {
        var first = new DisposableStored();
        var second = new DisposableStored();
        _registry.Register(first);
        _registry.Register(second);

        await _registry.DisposeAsync();

        first.Disposed.Should().BeTrue();
        second.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_does_not_dispose_an_object_that_was_already_removed()
    {
        var stored = new DisposableStored();
        var registered = _registry.Register(stored);
        _registry.Remove(registered.Id);

        await _registry.DisposeAsync();

        stored.Disposed.Should().BeFalse();
    }

    private class Stored;

    private class OtherStored;

    private class DisposableStored : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
