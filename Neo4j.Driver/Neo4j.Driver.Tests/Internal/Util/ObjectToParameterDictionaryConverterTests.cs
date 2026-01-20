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
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Util;
using Neo4j.Driver.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Util;

public class ObjectToParameterDictionaryConverterTests
{
    private readonly ObjectToParameterDictionaryConverter _converter = new();

    [Fact]
    public void ShouldReturnNullGivenNull()
    {
        var dict = _converter.Convert(null);
        dict.Should().BeNull();
    }

    [Theory]
    [InlineData((sbyte)0)]
    [InlineData((byte)1)]
    [InlineData((short)2)]
    [InlineData((ushort)3)]
    [InlineData(4)]
    [InlineData((uint)5)]
    [InlineData((long)6)]
    [InlineData((ulong)7)]
    [InlineData((char)8)]
    [InlineData((float)9)]
    [InlineData((double)10)]
    [InlineData(true)]
    public void ShouldHandleSimpleTypes(object value)
    {
        var dict = _converter.Convert(new { key = value });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        dict.Should().ContainValue(value);
    }

    [Fact]
    public void ShouldHandleString()
    {
        var dict = _converter.Convert(new { key = "value" });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        dict.Should().ContainValue("value");
    }

    [Fact]
    public void ShouldHandleArray()
    {
        var array = new byte[2];
        var dict = _converter.Convert(new { key = array });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        dict.Should().ContainValue(array);
    }

    [Fact]
    public void ShouldHandleAnonymousObjects()
    {
        var dict = _converter.Convert(new { key1 = "value1", key2 = "value2" });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(2);
        dict.Should()
            .Contain(
                new KeyValuePair<string, object>("key1", "value1"),
                new KeyValuePair<string, object>("key2", "value2"));
    }

    [Fact]
    public void ShouldHandleVectors()
    {
        var vector = Vector.Create([1.0, 2.0, 3.0]);
        var dict = _converter.Convert(new { vector });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("vector");
        dict["vector"].Should().BeOfType<Vector<double>>();
        ((Vector<double>)dict["vector"]).Values.Should().BeEquivalentTo([1.0, 2.0, 3.0]);
    }

    [Fact]
    public void ShouldHandlePoco()
    {
        var dict = _converter.Convert(new MyPoco { Key1 = "value1", Key2 = "value2" });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(2);
        dict.Should()
            .Contain(
                new KeyValuePair<string, object>("Key1", "value1"),
                new KeyValuePair<string, object>("Key2", "value2"));
    }

    [Fact]
    public void ShouldHandleDeeperObjects()
    {
        var dict = _converter.Convert(new { InnerObject = new { Key1 = 1, Key2 = "a", Key3 = 0L } });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerObject");
        var innerObjectObject = dict["InnerObject"];
        innerObjectObject.Should().NotBeNull();
        innerObjectObject.Should().BeAssignableTo<IDictionary<string, object>>();
        var innerObject = (IDictionary<string, object>)innerObjectObject;
        innerObject.Should()
            .Contain(
                new KeyValuePair<string, object>("Key1", 1),
                new KeyValuePair<string, object>("Key2", "a"),
                new KeyValuePair<string, object>("Key3", 0L));
    }

    [Fact]
    public void ShouldHandleDictionary()
    {
        var dict = _converter.Convert(
            new
            {
                InnerDictionary = new Dictionary<string, object>
                {
                    { "Key1", 1 },
                    { "Key2", "a" },
                    { "Key3", 0L }
                }
            });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerDictionary");
        var innerDictionaryObject = dict["InnerDictionary"];
        innerDictionaryObject.Should().NotBeNull();
        innerDictionaryObject.Should().BeAssignableTo<IDictionary<string, object>>();
        var innerDictionary = (IDictionary<string, object>)innerDictionaryObject;
        innerDictionary.Should()
            .Contain(
                new KeyValuePair<string, object>("Key1", 1),
                new KeyValuePair<string, object>("Key2", "a"),
                new KeyValuePair<string, object>("Key3", 0L));
    }

    [Fact]
    public void ShouldHandleCollections()
    {
        var dict = _converter.Convert(new { InnerCollection = new List<int> { 1, 2, 3 } });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerCollection");
        var innerCollectionObject = dict["InnerCollection"];
        innerCollectionObject.Should().NotBeNull();
        innerCollectionObject.Should().BeAssignableTo<IList<int>>();
        var innerCollection = (IList<int>)innerCollectionObject;
        innerCollection.Should().Contain(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ShouldHandleCollectionsOfArbitraryObjects()
    {
        var dict = _converter.Convert(
            new
            {
                InnerCollection = new List<object>
                {
                    new { a = "a" },
                    3,
                    new MyPoco { Key1 = "value1" }
                }
            });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerCollection");
        var innerCollectionObject = dict["InnerCollection"];
        innerCollectionObject.Should().NotBeNull();
        innerCollectionObject.Should().BeAssignableTo<IList<object>>();
        var innerCollection = (IList<object>)innerCollectionObject;
        innerCollection.Should().HaveCount(3);
        innerCollection.Should()
            .Contain(o => o is IDictionary<string, object> &&
                ((IDictionary<string, object>)o).Contains(new KeyValuePair<string, object>("a", "a")));

        innerCollection.Should().Contain(3);
        innerCollection.Should()
            .Contain(o => o is IDictionary<string, object> &&
                ((IDictionary<string, object>)o).Contains(new KeyValuePair<string, object>("Key1", "value1")));
    }

    [Fact]
    public void ShouldHandleDictionaryOfArbitraryObjects()
    {
        var dict = _converter.Convert(
            new
            {
                InnerDictionary = new Dictionary<string, object>
                {
                    { "a", new { a = "a" } },
                    { "b", "b" },
                    { "c", 3 }
                }
            });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerDictionary");
        var innerDictionaryObject = dict["InnerDictionary"];
        innerDictionaryObject.Should().NotBeNull();
        innerDictionaryObject.Should().BeAssignableTo<IDictionary<string, object>>();
        var innerDictionary = (IDictionary<string, object>)innerDictionaryObject;
        innerDictionary.Should().HaveCount(3);
        innerDictionary.Should().ContainKey("a");
        innerDictionary["a"].Should().BeAssignableTo<IDictionary<string, object>>();
        innerDictionary["a"]
            .As<IDictionary<string, object>>()
            .Should()
            .Contain(new KeyValuePair<string, object>("a", "a"));

        innerDictionary.Should().Contain(new KeyValuePair<string, object>("b", "b"));
        innerDictionary.Should().Contain(new KeyValuePair<string, object>("c", 3));
    }

    [Fact]
    public void ShouldRaiseExceptionWhenDictionaryKeysAreNotStrings()
    {
        var ex = Record.Exception(() => _converter.Convert(
            new
            {
                InnerDictionary = new Dictionary<int, object>
                {
                    { 1, new { a = "a" } },
                    { 2, "b" },
                    { 3, 3 }
                }
            }));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<InvalidOperationException>();
        ex.Message.Should().Contain("string keys");
    }

    [Fact]
    public void ShouldHandleListOfArbitraryObjects()
    {
        var dict = _converter.Convert(
            new
            {
                InnerList = new List<object>
                {
                    new { a = "a" },
                    "b",
                    3
                }
            });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerList");
        var innerListObject = dict["InnerList"];
        innerListObject.Should().NotBeNull();
        innerListObject.Should().BeAssignableTo<IList<object>>();
        var innerList = (IList<object>)innerListObject;
        innerList.Should().HaveCount(3);
        innerList[0].Should().BeAssignableTo<IDictionary<string, object>>();
        innerList[0].As<IDictionary<string, object>>().Should().Contain(new KeyValuePair<string, object>("a", "a"));
        innerList[1].Should().Be("b");
        innerList[2].As<int>().Should().Be(3);
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public void ToDictionary_ShouldHandleEmptyDictionary()
    {
        var emptyDictionary = new Dictionary<string, Person>();
        var result = _converter.Convert(emptyDictionary);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToDictionary_ShouldConvertDictionaryWithSimpleObjectsCorrectly()
    {
        var sourceDictionary = new Dictionary<string, Person>
        {
            { "Key1", new Person { Name = "John", Age = 30 } },
            { "Key2", new Person { Name = "Jane", Age = 25 } }
        };

        var result = _converter.Convert(sourceDictionary);
        result.Should().HaveCount(2);
        result["Key1"].Should().BeEquivalentTo(sourceDictionary["Key1"]);
        result["Key2"].Should().BeEquivalentTo(sourceDictionary["Key2"]);
    }

    [Fact]
    public void ToDictionary_ShouldReturnNullForNullDictionary()
    {
        Dictionary<string, Person> nullDictionary = null;
        var actual = _converter.Convert(nullDictionary);
        actual.Should().BeNull();
    }

    [Fact]
    public void ToDictionary_ShouldHandleNestedDictionaryCorrectly()
    {
        var nestedDictionary = new Dictionary<string, Dictionary<string, Person>>
        {
            {
                "Nested", new Dictionary<string, Person>
                {
                    { "InnerKey", new Person { Name = "Doe", Age = 40 } }
                }
            }
        };

        var result = _converter.Convert(nestedDictionary);
        result.Should().ContainKey("Nested");
        var innerDict = result["Nested"].As<Dictionary<string, Person>>();
        innerDict.Should().ContainKey("InnerKey");
        innerDict["InnerKey"].Should().BeEquivalentTo(new Person { Name = "Doe", Age = 40 });
    }

    [Fact]
    public void ShouldHandleEnumerable()
    {
        var array = new[] { 1, 2, 3 };
        var value = new MyCollection<int>(array);
        var dict = _converter.Convert(new { key = value });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        var s = dict["key"].ToContentString();
        s.Should().Be("[1, 2, 3]");
    }

    [Fact]
    public void ShouldHandleEnumerableOfEnumerable()
    {
        var array = new[] { 1, 2, 3 };
        IEnumerable element = new MyCollection<int>(array);
        var value = new MyCollection<object>(new[] { element, "a" });
        var dict = _converter.Convert(new { key = value });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        var s = dict["key"].ToContentString();
        s.Should().Be("[[1, 2, 3], a]");
    }


    [Fact]
    public void ShouldObserveParameterMappingAttribute()
    {
        var dict = _converter.Convert(
            new ClassWithMappingAttributes { SomeProperty = "someValue" });

        dict.Should().ContainKey("someParameter");
        dict["someParameter"].Should().Be("someValue");
    }

    private class MyPoco
    {
        public string Key1 { get; set; }
        public string Key2 { get; set; }
    }

    private class ClassWithMappingAttributes
    {
        [ParameterMapping("someParameter")]
        public string SomeProperty { get; init; }

        public string DifferentlyCased { get; init; }
    }

    public class ClassWithDotInMappingSourceAndParameterMapping
    {
        [MappingSource("object.property")]
        [ParameterMapping("mappedProperty")]
        public string PropertyWithDotInMappingSource { get; init; }
    }
    public class MyCollection<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _values;

        public MyCollection(IEnumerable<T> values)
        {
            _values = values;
        }

        public string Name => "My Collection implements IEnumerable<T>";
        public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
