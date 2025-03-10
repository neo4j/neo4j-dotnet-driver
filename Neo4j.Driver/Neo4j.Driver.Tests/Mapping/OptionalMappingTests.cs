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

using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class OptionalMappingTests
{
    [Fact]
    public void ShouldNotThrowIfOptionalPropertyIsNotPresent()
    {
        var record = TestRecord.Create(["someField"], [69]);
        var mapped = record.AsObject<ClassWithOptionalProperty>();

        mapped.Value.Should().Be(1234);
    }

    [Fact]
    public void ShouldUseDefaultValueIfOptionalPropertyIsNotPresent()
    {
        var record = TestRecord.Create(["someField"], [69]);
        var mapped = record.AsObject<ClassWithPropertyWithDefaultValue>();

        mapped.Value.Should().Be(42);
    }

    [Fact]
    public void ShouldMapConstructorParameterWithDefaultValue()
    {
        var record = TestRecord.Create(["someField"], [69]);
        var mapped = record.AsObject<ClassWithConstructorParameterWithDefaultValue>();

        mapped.Value.Should().Be(42);
    }

    [Fact]
    public void ShouldNotUseDefaultValueIfPropertyIsPresent()
    {
        var record = TestRecord.Create(["Value"], [69]);
        var mapped = record.AsObject<ClassWithPropertyWithDefaultValue>();

        mapped.Value.Should().Be(69);
    }

    [Fact]
    public void ShouldNotUseDefaultValueIfConstructorParameterIsPresent()
    {
        var record = TestRecord.Create(["value"], [69]);
        var mapped = record.AsObject<ClassWithConstructorParameterWithDefaultValue>();

        mapped.Value.Should().Be(69);
    }

    [Fact]
    public void ShouldFailToCreateObject()
    {
        var record = TestRecord.Create(["NotTheValue"], [69]);
        var act = () => { _ = record.AsObject<ClassWithPropertyWithoutDefaultValue>(); };

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailToCreateList()
    {
        var records = new[]
        {
            TestRecord.Create(["Value"], [1]),
            TestRecord.Create(["NotTheValue"], [2]),
            TestRecord.Create(["Value"], [3])
        };

        var act = () => { _ = records.Select(r => r.AsObject<ClassWithPropertyWithoutDefaultValue>()).ToList(); };

        act.Should().Throw<MappingFailedException>();
    }

    private class ClassWithOptionalProperty
    {
        [MappingOptional]
        public int Value { get; } = 1234;
    }

    private class ClassWithPropertyWithDefaultValue
    {
        [MappingDefaultValue(42)]
        public int Value { get; set; }
    }

    private class ClassWithConstructorParameterWithDefaultValue
    {
        public ClassWithConstructorParameterWithDefaultValue([MappingDefaultValue(42)] int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private class ClassWithPropertyWithoutDefaultValue
    {
        public int Value { get; set; }
    }
}
