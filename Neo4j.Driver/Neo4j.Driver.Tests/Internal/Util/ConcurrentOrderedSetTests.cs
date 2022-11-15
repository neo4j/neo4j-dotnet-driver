// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Linq;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.Util;

public static class ConcurrentOrderedSetTests
{
    public class AddMethod
    {
        [Fact]
        public void ShouldAddNew()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var set = new ConcurrentOrderedSet<int>();
            set.Add(1);
            set.Count.Should().Be(1);
            set.ToList().Should().ContainInOrder(1);
        }

        [Fact]
        public void ShouldNotAddIfAlreadyExists()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2, 3 };
            set.Add(0);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Count.Should().Be(4);
            set.Should().ContainInOrder(0, 1, 2, 3);
        }
    }

    public class RemoveMethod
    {
        [Fact]
        public void ShouldRemove()
        {
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2, 3 };
            set.Remove(0);
            set.Remove(2);
            set.ToList().Should().ContainInOrder(1, 3);
        }

        [Fact]
        public void ShouldNotMoveIfNotExists()
        {
            var set = new ConcurrentOrderedSet<int> { 0, 1 };
            set.Remove(3);
            set.Count.Should().Be(2);
            set.ToList().Should().ContainInOrder(0, 1);
        }
    }

    public class ClearMethod
    {
        [Fact]
        public void ShouldClear()
        {
            // Given
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2, 3 };

            set.Should().NotBeEmpty();

            // When
            set.Clear();

            // Then
            set.Should().BeEmpty();
        }
    }

    public class SnapshotMethod
    {
        [Fact]
        public void ShouldProvideSnapshotWhenEmpty()
        {
            // Given
            var set = new ConcurrentOrderedSet<int>();

            // When
            var snapshot = set.Snapshot;

            // Then
            snapshot.Should().BeEmpty();
        }

        [Fact]
        public void ShouldProvideSnapshot()
        {
            // Given
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2 };

            // When
            var snapshot = set.Snapshot;

            // Then
            snapshot.Should().HaveCount(3);
            snapshot.Should().ContainInOrder(0, 1, 2);
        }

        [Fact]
        public void ShouldProvideSnapshotAfterUpdate()
        {
            // Given
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2 };

            // When
            set.Remove(1);
            set.Add(42);
            var snapshot = set.Snapshot;

            // Then
            snapshot.Should().HaveCount(3);
            snapshot.Should().ContainInOrder(0, 2, 42);
        }
    }

    public class ConcurrentAccessTests
    {
        [Theory]
        [InlineData(3)]
        [InlineData(30)]
        public void ShouldBeAbleToAccessNewlyAddedItem(int times)
        {
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2, 3 };

            // we loop several turns on the full set
            for (var j = 0; j < times; j++)
            {
                for (var i = 0; i < set.Count; i++)
                {
                    var real = set.ToList()[i];
                    real.Should().Be(i);
                }
            }

            // we add a new item into the set
            set.Add(4);

            // we loop again and everything is in set
            for (var j = 0; j < times; j++)
            {
                // first we got the newly added out
                set.Contains(4).Should().BeTrue();

                for (var i = 0; i < set.Count; i++)
                {
                    var real = set.ToList()[i];
                    real.Should().Be(i);
                }
            }
        }

        [Theory]
        [InlineData(3)]
        [InlineData(40)]
        public void ShouldBeAbleToRemoveItem(int times)
        {
            var set = new ConcurrentOrderedSet<int> { 0, 1, 2, 3 };
            for (var j = 0; j < times; j++)
            {
                for (var i = 0; i < set.Count; i++)
                {
                    var real = set.ToList()[i];
                    real.Should().Be(i);
                }
            }

            set.Remove(3);

            for (var j = 0; j < times; j++)
            {
                set.Contains(3).Should().BeFalse();

                for (var i = 0; i < set.Count; i++)
                {
                    var real = set.ToList()[i];
                    real.Should().Be(i);
                }
            }
        }
    }
}
