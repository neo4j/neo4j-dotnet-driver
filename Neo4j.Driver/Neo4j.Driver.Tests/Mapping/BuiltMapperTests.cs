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
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class BuiltMapperTests
{
    [Fact]
    public void ShouldThrowIfNoParameterlessConstructor()
    {
        var mapper = new BuiltMapper<NoParameterlessConstructor>();
        var act = () => mapper.Map(null);
        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldUseConstructorWhenInstructed()
    {
        var mapper = new BuiltMapper<NoParameterlessConstructor>();

        var constructor = typeof(NoParameterlessConstructor).GetConstructors()[0];
        mapper.AddConstructorMapping(constructor);
        var result = mapper.Map(TestRecord.Create(new[] { "value" }, new object[] { 48 }));
        result.Value.Should().Be(48);
    }

    [Fact]
    public void ShouldMapProperties()
    {
        var builder = new MappingBuilder<TwoPropertyClass>();
        builder.Map(x => x.Value1, "value1");
        builder.Map(x => x.Value2, "value2");

        var record = TestRecord.Create(["value1", "value2"], [42, 43]);
        var mapper = builder.Build();
        var result = mapper.Map(record);
        result.Value1.Should().Be(42);
        result.Value2.Should().Be(43);
    }

    [Fact]
    public void ShouldThrowWhenPropertyNotFoundInRecord()
    {
        var builder = new MappingBuilder<TwoPropertyClass>();
        builder.Map(x => x.Value1, "value1");
        builder.Map(x => x.Value2, "value2");

        var record = TestRecord.Create(["value1"], [42]);
        var mapper = builder.Build();
        var act = () => mapper.Map(record);
        act.Should().Throw<MappingFailedException>();
    }

    private class NoParameterlessConstructor
    {
        public NoParameterlessConstructor(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private class TwoPropertyClass
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }
}
