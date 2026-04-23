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

using System;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests;

public class ListAddressResolverTests
{
    public class AddStringMethod
    {
        [Fact]
        public void ShouldThrowWhenHostAndPortIsNull()
        {
            var resolver = new ListAddressResolver();
            var act = () => resolver.Add((string)null);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ShouldThrowWhenHostAndPortIsEmpty()
        {
            var resolver = new ListAddressResolver();
            var act = () => resolver.Add(string.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ShouldThrowWhenHostIsEmpty()
        {
            var resolver = new ListAddressResolver();
            var act = () => resolver.Add(":7687");
            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void ShouldThrowWhenPortIsZero()
        {
            var resolver = new ListAddressResolver();
            var act = () => resolver.Add("localhost:0");
            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void ShouldThrowWhenPortIsNegative()
        {
            var resolver = new ListAddressResolver();
            var act = () => resolver.Add("localhost:-1");
            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void ShouldThrowWhenPortExceedsMaximum()
        {
            var resolver = new ListAddressResolver();
            var act = () => resolver.Add("localhost:65536");
            act.Should().Throw<FormatException>();
        }
    }
}
