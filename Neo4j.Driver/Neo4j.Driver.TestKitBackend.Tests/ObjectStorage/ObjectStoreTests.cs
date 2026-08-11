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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.ObjectStorage;

public class ObjectStoreTests
{
    private readonly ObjectStore _objectStore = AutoMocker.ForTesting<ObjectStore>().CreateInstance<ObjectStore>();

    [Fact]
    public void Register_returns_a_objectStore_object_carrying_the_registered_object()
    {
        var stored = new Stored();

        var registered = _objectStore.Register(stored);

        registered.Object.Should().BeSameAs(stored);
        registered.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Register_assigns_distinct_ids()
    {
        var first = _objectStore.Register(new Stored());
        var second = _objectStore.Register(new Stored());

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public void Register_with_a_factory_passes_the_id_the_object_will_be_stored_under()
    {
        string? idGivenToFactory = null;
        Stored? created = null;

        var registered = _objectStore.Register(id =>
        {
            idGivenToFactory = id;
            created = new Stored();
            return created;
        });

        idGivenToFactory.Should().Be(registered.Id);
        registered.Object.Should().BeSameAs(created);
        _objectStore.Get<Stored>(registered.Id).Object.Should().BeSameAs(created);
    }

    [Fact]
    public void Get_returns_the_registered_object_under_its_id()
    {
        var stored = new Stored();
        var registered = _objectStore.Register(stored);

        var got = _objectStore.Get<Stored>(registered.Id);

        got.Object.Should().BeSameAs(stored);
        got.Id.Should().Be(registered.Id);
    }

    [Fact]
    public void Get_throws_for_an_unknown_id()
    {
        var get = () => _objectStore.Get<Stored>("no-such-id");

        get.Should().Throw<TestKitProtocolException>().WithMessage("*no-such-id*");
    }

    [Fact]
    public void Get_throws_when_the_id_belongs_to_an_object_of_a_different_type()
    {
        var registered = _objectStore.Register(new Stored());

        var get = () => _objectStore.Get<OtherStored>(registered.Id);

        get.Should().Throw<TestKitProtocolException>().WithMessage($"*{registered.Id}*");
    }

    [Fact]
    public void Remove_makes_the_id_unknown()
    {
        var registered = _objectStore.Register(new Stored());

        _objectStore.Remove(registered.Id);

        var get = () => _objectStore.Get<Stored>(registered.Id);
        get.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public async Task DisposeAsync_disposes_every_registered_disposable_object()
    {
        var first = new DisposableStored();
        var second = new DisposableStored();
        _objectStore.Register(first);
        _objectStore.Register(second);

        await _objectStore.DisposeAsync();

        first.Disposed.Should().BeTrue();
        second.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_disposes_in_reverse_registration_order()
    {
        var disposalOrder = new List<string>();
        _objectStore.Register(new SequencedDisposable("first", disposalOrder));
        _objectStore.Register(new SequencedDisposable("second", disposalOrder));
        _objectStore.Register(new SequencedDisposable("third", disposalOrder));

        await _objectStore.DisposeAsync();

        disposalOrder.Should().Equal("third", "second", "first");
    }

    [Fact]
    public async Task DisposeAsync_continues_past_a_throwing_disposal_and_still_clears_the_objectStore()
    {
        var throwing = new ThrowingDisposable();
        _objectStore.Register(throwing);
        var second = new DisposableStored();
        var registeredSecond = _objectStore.Register(second);

        var act = () => _objectStore.DisposeAsync().AsTask();
        await act.Should().NotThrowAsync();

        second.Disposed.Should().BeTrue();

        var get = () => _objectStore.Get<DisposableStored>(registeredSecond.Id);
        get.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public async Task DisposeAsync_does_not_dispose_an_object_that_was_already_removed()
    {
        var stored = new DisposableStored();
        var registered = _objectStore.Register(stored);
        _objectStore.Remove(registered.Id);

        await _objectStore.DisposeAsync();

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

    private class ThrowingDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            throw new InvalidOperationException("boom");
        }
    }

    private class SequencedDisposable : IAsyncDisposable
    {
        private readonly string _name;
        private readonly List<string> _disposalOrder;

        public SequencedDisposable(string name, List<string> disposalOrder)
        {
            _name = name;
            _disposalOrder = disposalOrder;
        }

        public ValueTask DisposeAsync()
        {
            _disposalOrder.Add(_name);
            return ValueTask.CompletedTask;
        }
    }
}
