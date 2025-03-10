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

using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class MappableValueProviderTests
{
    private readonly AutoMocker _mocker = new();

    [Fact]
    public void TryGetMappableValueShouldReturnFalseWhenFieldNotFound()
    {
        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.TryGetMappableValue(
            Mock.Of<IRecord>(),
            _ => null,
            typeof(object),
            out var _);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldReturnSimpleValue()
    {
        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.TryGetMappableValue(
            Mock.Of<IRecord>(),
            _ => "test-value",
            typeof(string),
            out var value);

        result.Should().BeTrue();
        value.Should().Be("test-value");
    }

    [Fact]
    public void ShouldApplyMappingToEntities()
    {
        var entity = new Node(
            0,
            new[] { "person" },
            new Dictionary<string, object> { { "name", "Bob" }, { "age", 42 } });

        _mocker.GetMock<IRecordObjectMapping>()
            .Setup(x => x.Map(It.Is<DictAsRecord>(d => d["name"].As<string>() == "Bob"), typeof(Person)))
            .Returns(new Person { Name = "Mapped", Age = 24 });

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);
        var result = subject.TryGetMappableValue(
            Mock.Of<IRecord>(),
            _ => entity,
            typeof(Person),
            out var value);

        result.Should().BeTrue();
        value.Should().BeEquivalentTo(new Person { Name = "Mapped", Age = 24 });
    }

    [Fact]
    public void ShouldApplyMappingToDictionaries()
    {
        var dict = new Dictionary<string, object> { { "name", "Bob" }, { "age", 42 } };

        _mocker.GetMock<IRecordObjectMapping>()
            .Setup(x => x.Map(It.Is<DictAsRecord>(d => d["name"].As<string>() == "Bob"), typeof(Person)))
            .Returns(new Person { Name = "Mapped", Age = 24 });

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);
        var result = subject.TryGetMappableValue(
            Mock.Of<IRecord>(),
            _ => dict,
            typeof(Person),
            out var value);

        result.Should().BeTrue();
        value.Should().BeEquivalentTo(new Person { Name = "Mapped", Age = 24 });
    }

    private static MappingValueDelegate GetMockMappingDelegate(object value, bool returnValue)
    {
        return MockMap;

        bool MockMap(IRecord record, out object mappedValue)
        {
            mappedValue = value;
            return returnValue;
        }
    }

    [Fact]
    public void GetConvertedValueShouldReturnNullWhenFieldNotFound()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(null, false));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(object));

        result.Should().BeNull();
    }

    [Fact]
    public void ShouldReturnNullWhenValueIsNull()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(null, true));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(object));

        result.Should().BeNull();
    }

    [Fact]
    public void ShouldPrioritiseConverterIfProvided()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        var complexValue = new Dictionary<string, string> { { "key", "value" }, { "key2", "value2" } };

        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(complexValue, true));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(string),
            _ => "test-result");

        result.Should().Be("test-result");
    }

    [Fact]
    public void ShouldReturnEntitiesUnchanged()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        var complexValue = new Node(0, new[] { "label" }, new Dictionary<string, object> { { "key", "value" } });

        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(complexValue, true));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(Node));

        result.Should().Be(complexValue);
    }

    [Fact]
    public void ShouldReturnDictionariesUnchanged()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        var complexValue = new Dictionary<string, object> { { "key", "value" } };

        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(complexValue, true));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(Dictionary<string, object>));

        result.Should().Be(complexValue);
    }

    [Fact]
    public void ShouldCreateCommaSeparatedList()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        var numbers = new List<object> { 321, 654, 987 };

        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(numbers, true));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(string));

        result.Should().Be("321,654,987");
    }

    [Fact]
    public void ShouldCreateMappedList()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        var numbers = new List<object> { 321, 654, 987 };

        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(numbers, true));

        _mocker.GetMock<IMappedListCreator>()
            .Setup(x => x.CreateMappedList(numbers, typeof(List<string>), It.IsAny<IRecord>()))
            .Returns(new List<string> { "abc", "def", "ghi" });

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(List<string>));

        result.Should().BeEquivalentTo(new[] { "abc", "def", "ghi" });
    }

    [Fact]
    public void ShouldConvertValueToRequestedType()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        var value = 123;

        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(GetMockMappingDelegate(value, true));

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(string));

        result.Should().Be("123");
    }

    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }
}
