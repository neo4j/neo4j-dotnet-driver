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
using Neo4j.Driver.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class MappingBuilderTests
{
    [Fact]
    public void ShouldThrowIfNotAMemberExpression()
    {
        var subject = new MappingBuilder<TestClass>();
        var act = () => subject.Map(x => "something", "foo");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowIfNotAPropertyExpression()
    {
        var subject = new MappingBuilder<TestClass>();
        var act = () => subject.Map(x => x.NotAProperty, "foo");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowIfPropertyDoesNotHaveASetter()
    {
        var subject = new MappingBuilder<TestClass>();
        var act = () => subject.Map(x => x.NotSettable, "foo");
        act.Should().Throw<ArgumentException>();
    }

    private class TestClass
    {
        public readonly int NotAProperty = 0x6060B017;
        public int Settable { get; set; }
        public int NotSettable { get; } = -1;
    }
}
