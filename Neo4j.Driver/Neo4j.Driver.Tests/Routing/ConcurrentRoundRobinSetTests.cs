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
            [Fact]
            public void ShouldBeAbleToAccessNewlyAddedItem()
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                // we loop serveral turns on the full set
                for (var i = 0; i < 3*set.Count; i++)
                {
                    int real;
                    set.TryNext(out real).Should().BeTrue();
                    real.Should().Be(i%set.Count);
                }

                // we add a new item into the set
                set.Add(4);

                // we loop again and everything is in set
                for (var i = 0; i < 3*set.Count; i++)
                {
                    int real;
                    set.TryNext(out real).Should().BeTrue();
                    real.Should().Be(i % set.Count);
                }
            }

            [Fact]
            public void ShouldBeAbleToRemoveItem()
            {
                var set = new ConcurrentRoundRobinSet<int> {0, 1, 2, 3};

                for (var i = 0; i < 3 * set.Count; i++)
                {
                    int real;
                    set.TryNext(out real).Should().BeTrue();
                    var expect = i % set.Count;
                    real.Should().Be(expect);
                }

                set.Remove(3);

                for (var i = 0; i < 3 * set.Count; i++)
                {
                    int real;
                    set.TryNext(out real).Should().BeTrue();
                    var expect = i % set.Count;
                    real.Should().Be(expect);
                }
            }
        }
    }
}
