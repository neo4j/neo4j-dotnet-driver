// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ConcurrentRoundRobinSetTests
    {
        public class TryNextMethod
        {
            [Fact]
            public void ShouldReturnFalseIfNoElementInSet()
            {
                var set = new ConcurrentRoundRobinSet<int>();
                int value;
                set.TryNext(out value).Should().BeFalse();
                value.Should().Be(default(int));
            }

            [Fact]
            public void ShouldRoundRobin()
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                for (var i = 0; i < 10; i++)
                {
                    int real;
                    set.TryNext(out real).Should().BeTrue();
                    var expect = i % set.Count;
                    real.Should().Be(expect);
                }
            }
        }

        public class AddMethod
        {
            [Fact]
            public void ShouldAddNew()
            {
                // ReSharper disable once UseObjectOrCollectionInitializer
                var set = new ConcurrentRoundRobinSet<int>();
                set.Add(1);
                set.Count.Should().Be(1);
                set.ToList().Should().ContainInOrder(1);
            }
            [Fact]
            public void ShouldNotAddIfAlreadyExists()
            {
                // ReSharper disable once UseObjectOrCollectionInitializer
                var set = new ConcurrentRoundRobinSet<int> { 0, 1, 2, 3 };
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
                var set = new ConcurrentRoundRobinSet<int> { 0, 1, 2, 3 };
                set.Remove(0);
                set.Remove(2);
                set.ToList().Should().ContainInOrder(1, 3);
            }

            [Fact]
            public void ShouldNotMoveIfNotExists()
            {
                var set = new ConcurrentRoundRobinSet<int> { 0, 1 };
                set.Remove(3);
                set.Count.Should().Be(2);
                set.ToList().Should().ContainInOrder(0, 1);
            }
        }

        public class ClearMethod
        {
            [Fact]
            public void ShouldRemoveAdd()
            {
                var set = new ConcurrentRoundRobinSet<int> { 0, 1, 2, 3 };
                set.Clear();
                set.Should().BeEmpty();
            }
        }

        public class ConcurrentAccessTests
        {
            [Theory]
            [InlineData(3)]
            [InlineData(30)]
            public void ShouldBeAbleToAccessNewlyAddedItem(int times)
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                // we loop serveral turns on the full set
                for (var j = 0; j < times; j++)
                {
                    for (var i = 0; i < set.Count; i++)
                    {
                        int real;
                        set.TryNext(out real).Should().BeTrue();
                        real.Should().Be(i);
                    }
                }

                // we add a new item into the set
                set.Add(4);

                // we loop again and everything is in set
                for (var j = 0; j < times; j++)
                {
                    int real;

                    // first we got the newly added out
                    set.TryNext(out real).Should().BeTrue();
                    real.Should().Be(4);

                    for (var i = 0; i < set.Count - 1; i++)
                    {
                        set.TryNext(out real).Should().BeTrue();
                        real.Should().Be(i);
                    }
                }

            }

            [Theory]
            [InlineData(3)]
            [InlineData(40)]
            public void ShouldBeAbleToRemoveItem(int times)
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};
                for (var j = 0; j < times; j++)
                {
                    for (var i = 0; i < set.Count; i++)
                    {
                        int real;
                        set.TryNext(out real).Should().BeTrue();
                        real.Should().Be(i);
                    }
                }

                set.Remove(3);

                for (var j = 0; j < times; j++)
                {
                    for (var i = 0; i < set.Count; i++)
                    {
                        int real;
                        set.TryNext(out real).Should().BeTrue();
                        real.Should().Be(i);
                    }

                }
            }
        }
    }
}
