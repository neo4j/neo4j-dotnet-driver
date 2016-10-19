using System;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ConcurrentRoundRobinQueueTests
    {
        public class HopMethod
        {
            [Fact]
            public void ShouldThrowExceptionIfNoElementInSet()
            {
                var set = new ConcurrentRoundRobinSet<int>();
                var exception = Xunit.Record.Exception(() => set.Hop());
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Be("No item in set");
            }

            [Fact]
            public void ShouldRoundRobin()
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                for (var i = 0; i < 10; i++)
                {
                    var real = set.Hop();
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
                var set = new ConcurrentRoundRobinSet<int>();
                set.Add(1);
                set.Count.Should().Be(1);
                set.ToList().Should().ContainInOrder(1);
            }
            [Fact]
            public void ShouldNotAddIfAlreadyExists()
            {
                var set = new ConcurrentRoundRobinSet<int> { 0, 1, 2, 3 };
                set.Add(0);
                set.Add(1);
                set.Add(2);
                set.Add(3);
                set.Count.Should().Be(4);
                set.Should().ContainInOrder(0, 1, 2, 3);
            }
        }

        public class ClearMethod
        {
            [Fact]
            public void ShouldRemoveAll()
            {
                var set = new ConcurrentRoundRobinSet<int> { 0, 1, 2, 3 };
                set.Clear();
                set.Count.Should().Be(0);
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
                set.ToList().Should().ContainInOrder(3, 1);
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

        public class ConcurrentAccessTests
        {
            [Fact]
            public void ShouldBeAbleToAccessNewlyAddedItem()
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                // we loop serveral turns on the full set
                for (var i = 0; i < 3*set.Count; i++)
                {
                    set.Hop().Should().Be(i % set.Count);
                }

                // we add a new item into the set
                set.Add(4);

                // we loop again and everything is in set
                for (var i = 0; i < 3*set.Count; i++)
                {
                    set.Hop().Should().Be(i % set.Count);
                }
            }

            [Fact]
            public void ShouldBeAbleToRemoveItem()
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                for (var i = 0; i < 3 * set.Count; i++)
                {
                    var real = set.Hop();
                    var expect = i % set.Count;
                    real.Should().Be(expect);
                }

                set.Remove(3);

                for (var i = 0; i < 3 * set.Count; i++)
                {
                    var real = set.Hop();
                    var expect = i % set.Count;
                    real.Should().Be(expect);
                }
            }
        }
    }
}
