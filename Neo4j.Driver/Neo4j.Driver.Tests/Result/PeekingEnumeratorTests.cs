//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class PeekingEnumeratorTests
    {
        public class MoveNextMethod
        {
            [Fact]
            public void ShouldReturnItemIfExist()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> {"1", "2", "3"}.GetEnumerator());
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("1");
                enumerator.Position.Should().Be(0);
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("2");
                enumerator.Position.Should().Be(1);
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("3");
                enumerator.Position.Should().Be(2);
            }

            [Fact]
            public void ShouldReturnNullIfPassLast()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1"}.GetEnumerator());
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("1");
                enumerator.Position.Should().Be(0);
                enumerator.MoveNext().Should().BeFalse();
                enumerator.Current.Should().BeNull();
                enumerator.Position.Should().Be(1);
                // check again
                enumerator.MoveNext().Should().BeFalse();
                enumerator.Current.Should().BeNull();
                enumerator.Position.Should().Be(1);
            }
        }

        public class ResetMethod
        {
            [Fact]
            public void ShouldThrowExceptionIfTryToRevisit()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> {"1"}.GetEnumerator());
                var ex = Xunit.Record.Exception(()=>enumerator.Reset());
                ex.Should().BeOfType<InvalidOperationException>();
            }
        }

        public class PeekMethod
        {
            [Fact]
            public void ShouldReturnNextWithoutMoveingToNext()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1" }.GetEnumerator());

                enumerator.Peek().Should().Be("1");
                enumerator.Position.Should().Be(-1);
                enumerator.Peek().Should().Be("1"); // no matter how many times are called
                enumerator.Position.Should().Be(-1);

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("1");
                enumerator.Position.Should().Be(0);

            }

            [Fact]
            public void ShouldReturnNullIfNoMoreItems()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string>().GetEnumerator());
                var peeked = enumerator.Peek();
                peeked.Should().BeNull();
            }
        }

        public class ConsumeMethod
        {
            [Fact]
            public void ShouldConsumeAllItemsAfterDiscard()
            {
                var enumerator = new PeekingEnumerator<string>(new List<string> { "1", "2", "3" }.GetEnumerator());
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Consume();
                enumerator.MoveNext().Should().BeFalse();
                enumerator.Peek().Should().BeNull();

                enumerator.Position.Should().Be(3);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldNotDiscardAfterDispose()
            {
                var list = new List<string> { "1", "2", "3" }.GetEnumerator();
                var enumerator = new PeekingEnumerator<string>(list);
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Consume();
                enumerator.MoveNext().Should().BeFalse();
                enumerator.Peek().Should().BeNull();
                enumerator.Position.Should().Be(3);
            }
        }
    }
}