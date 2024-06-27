﻿// Copyright (c) "Neo4j"
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

public class MappingProviderTests
{
    private class TestObject
    {
        [MappingSource("intValue")]
        public int IntValue { get; set; }

        [MappingSource("stringValue")]
        public string Text { get; set; } = null!;
    }

    private class SecondTestObject
    {
        public int Number { get; set; }
        public string Text { get; set; } = null!;
    }

    private class ThirdTestObject
    {
        public int IntValue { get; set; } = -1;
        public string StringValue { get; set; } = "unset";
    }

    private class PersonWithAge
    {
        [MappingSource("name")]
        public string Name { get; set; } = null!;

        [MappingOptional]
        public int Age { get; set; }
    }

    private class TestMappingProvider : IMappingProvider
    {
        public void CreateMappers(IMappingRegistry registry)
        {
            registry
                .RegisterMapping<TestObject>(
                    b => b
                        .UseDefaultMapping()
                        .Map(x => x.Text, "stringValue", converter: x => x.As<string>().ToUpper() + "!"))
                .RegisterMapping<SecondTestObject>(
                    b => b
                        .MapWholeObject(
                            r => new SecondTestObject
                            {
                                Number = r.Get<int>("intValue") + 1,
                                Text = r.Get<string>("stringValue").ToLower()
                            }))
                .RegisterMapping<ThirdTestObject>(_ => {})
                .RegisterMapping<PersonWithAge>(
                    b => b
                        .UseDefaultMapping()
                        .Map(x => x.Age, r => r.Get<int>("active") - r.Get<int>("born")));
        }
    }

    public MappingProviderTests()
    {
        RecordObjectMapping.Reset();
    }

    [Fact]
    public void ShouldOverrideDefaultMapping()
    {
        var record = TestRecord.Create(new[] { "stringValue", "intValue" }, new object[] { "test", 69 });
        RecordObjectMapping.RegisterProvider<TestMappingProvider>();

        var obj = record.AsObject<TestObject>();

        obj.Text.Should().Be("TEST!");
        obj.IntValue.Should().Be(69);
    }

    [Fact]
    public void ShouldUseWholeObjectMapping()
    {
        var record = TestRecord.Create(new[] { "stringValue", "intValue" }, new object[] { "TEST", 100 });
        RecordObjectMapping.RegisterProvider<TestMappingProvider>();

        var obj = record.AsObject<SecondTestObject>();

        obj.Text.Should().Be("test");
        obj.Number.Should().Be(101);
    }

    [Fact]
    public void ShouldNotUseDefaultMapperIfEmptyMappingConfigInProvider()
    {
        var record = TestRecord.Create(new[] { "stringValue", "intValue" }, new object[] { "TEST", 100 });
        RecordObjectMapping.RegisterProvider<TestMappingProvider>();

        var obj = record.AsObject<ThirdTestObject>();

        obj.StringValue.Should().Be("unset");
        obj.IntValue.Should().Be(-1);
    }

    [Fact]
    public void ShouldMapPropertiesFromRecordIfRequired()
    {
        var record = TestRecord.Create(new[] { "name", "born", "active" }, new object[] { "Bob", 1977, 2000 });
        RecordObjectMapping.RegisterProvider<TestMappingProvider>();

        var obj = record.AsObject<PersonWithAge>();

        obj.Name.Should().Be("Bob");
        obj.Age.Should().Be(23);
    }
}
