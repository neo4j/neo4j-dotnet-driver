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
    public void Store_returns_the_id_the_object_is_stored_under()
    {
        var stored = new Stored();

        var id = _objectStore.Store(stored);

        id.Should().NotBeNullOrEmpty();
        _objectStore.Get<Stored>(id).Should().BeSameAs(stored);
    }

    [Fact]
    public void Store_assigns_distinct_ids()
    {
        var first = _objectStore.Store(new Stored());
        var second = _objectStore.Store(new Stored());

        second.Should().NotBe(first);
    }

    [Fact]
    public void Store_with_a_factory_passes_the_id_the_object_will_be_stored_under()
    {
        string? idGivenToFactory = null;
        Stored? created = null;

        var storedId = _objectStore.Store(id =>
        {
            idGivenToFactory = id;
            created = new Stored();
            return created;
        });

        idGivenToFactory.Should().Be(storedId);
        _objectStore.Get<Stored>(storedId).Should().BeSameAs(created);
    }

    [Fact]
    public void Get_returns_the_stored_object_under_its_id()
    {
        var stored = new Stored();
        var id = _objectStore.Store(stored);

        _objectStore.Get<Stored>(id).Should().BeSameAs(stored);
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
        var id = _objectStore.Store(new Stored());

        var get = () => _objectStore.Get<OtherStored>(id);

        get.Should().Throw<TestKitProtocolException>().WithMessage($"*{id}*");
    }

    [Fact]
    public void Remove_makes_the_id_unknown()
    {
        var id = _objectStore.Store(new Stored());

        _objectStore.Remove(id);

        var get = () => _objectStore.Get<Stored>(id);
        get.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public async Task Store_is_safe_under_concurrent_access()
    {
        const int workerCount = 8;
        const int storesPerWorker = 2000;

        var preStored = Enumerable.Range(0, 100).Select(_ => _objectStore.Store(new Stored())).ToArray();

        var storeTasks = Enumerable.Range(0, workerCount)
            .Select(
                _ => Task.Run(
                    () =>
                    {
                        var ids = new string[storesPerWorker];
                        for (var i = 0; i < storesPerWorker; i++)
                        {
                            ids[i] = _objectStore.Store(new Stored());
                        }

                        return ids;
                    },
                    TestContext.Current.CancellationToken))
            .ToArray();

        var getTask = Task.Run(
            () =>
            {
                for (var i = 0; i < storesPerWorker; i++)
                {
                    foreach (var id in preStored)
                    {
                        _objectStore.Get<Stored>(id);
                    }
                }
            },
            TestContext.Current.CancellationToken);

        await Task.WhenAll(storeTasks.Cast<Task>().Append(getTask));

        var allIds = storeTasks.SelectMany(t => t.Result).ToArray();

        allIds.Should().HaveCount(workerCount * storesPerWorker);
        allIds.Should().OnlyHaveUniqueItems();

        foreach (var id in allIds)
        {
            _objectStore.Get<Stored>(id);
        }
    }

    [Fact]
    public async Task DisposeAsync_disposes_every_stored_disposable_object()
    {
        var first = new DisposableStored();
        var second = new DisposableStored();
        _objectStore.Store(first);
        _objectStore.Store(second);

        await _objectStore.DisposeAsync();

        first.Disposed.Should().BeTrue();
        second.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_disposes_in_reverse_storage_order()
    {
        var disposalOrder = new List<string>();
        _objectStore.Store(new SequencedDisposable("first", disposalOrder));
        _objectStore.Store(new SequencedDisposable("second", disposalOrder));
        _objectStore.Store(new SequencedDisposable("third", disposalOrder));

        await _objectStore.DisposeAsync();

        disposalOrder.Should().Equal("third", "second", "first");
    }

    [Fact]
    public async Task DisposeAsync_continues_past_a_throwing_disposal_and_still_clears_the_objectStore()
    {
        var throwing = new ThrowingDisposable();
        _objectStore.Store(throwing);
        var second = new DisposableStored();
        var secondId = _objectStore.Store(second);

        var act = () => _objectStore.DisposeAsync().AsTask();
        await act.Should().NotThrowAsync();

        second.Disposed.Should().BeTrue();

        var get = () => _objectStore.Get<DisposableStored>(secondId);
        get.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public async Task DisposeAsync_does_not_dispose_an_object_that_was_already_removed()
    {
        var stored = new DisposableStored();
        var id = _objectStore.Store(stored);
        _objectStore.Remove(id);

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
