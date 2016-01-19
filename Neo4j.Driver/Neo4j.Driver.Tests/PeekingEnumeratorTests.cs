using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class PeekingEnumeratorTests
    {
        public class NextMethod
        {
            [Fact]
            public void ShouldReturnItemIfExisit()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> {"1", "2", "3"}.GetEnumerator());
                enumerator.Next().Should().Be("1");
                enumerator.Next().Should().Be("2");
                enumerator.Next().Should().Be("3");
            }

            [Fact]
            public void ShouldReturnNullIfPassLast()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1"}.GetEnumerator());
                enumerator.Next().Should().Be("1");
                enumerator.Next().Should().BeNull();
            }
        }

        public class HasNextMethod
        {
            [Fact]
            public void ShouldReturnTrueIfHasNext()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1" }.GetEnumerator());
                enumerator.HasNext().Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnFalseIfHasNoNext()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string>().GetEnumerator());
                enumerator.HasNext().Should().BeFalse();
            }
        }

        public class PeekMethod
        {
            [Fact]
            public void ShouldReturnNextWithoutMoveingToNext()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1" }.GetEnumerator());
                var peeked = enumerator.Peek();
                peeked.Should().NotBeNull();
                peeked.Should().Be("1");
                enumerator.HasNext().Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnNullIfNoMoreItems()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string>().GetEnumerator());
                var peeked = enumerator.Peek();
                peeked.Should().BeNull();
            }
        }

        public class DiscardMethod
        {
            [Fact]
            public void ShouldDiscardAllItemsAfterDiscard()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1", "2", "3" }.GetEnumerator());
                enumerator.HasNext().Should().BeTrue();
                enumerator.Discard();
                enumerator.HasNext().Should().BeFalse();
            }
        }
    }
}