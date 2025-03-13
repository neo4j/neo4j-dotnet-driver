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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using CollectionExtensions = Neo4j.Driver.Internal.CollectionExtensions;

namespace Neo4j.Driver.Tests.TestUtil;

public class CollectionExtensionsTests
{
    public class ToContentString
    {
        [Fact]
        public void ShouldConvertListToStringCorrectly()
        {
            var list = new List<object> { "a", 2, new[] { 3, 4 } };
            list.ToContentString().Should().Be("[a, 2, [3, 4]]");
        }

        [Fact]
        public void ShouldConvertDictionaryToStringCorrectly()
        {
            var dict = new Dictionary<string, object>
            {
                { "a", "a" },
                { "b", 2 },
                { "c", new[] { 1, 2, 3 } }
            };

            dict.ToContentString().Should().Be("[{a, a}, {b, 2}, {c, [1, 2, 3]}]");
        }
    }

    public class GetValueMethod
    {
        [Fact]
        public void ShouldGetDefaultValueCorrectly()
        {
            var dict = new Dictionary<string, object>();
            var defaultValue = 10;
            dict.GetValue("any", defaultValue).Should().Be(defaultValue);
        }

        [Fact]
        public void ShouldGetValueCorrectlyWhenExpectingInt()
        {
            object o = 10;
            var dict = new Dictionary<string, object> { { "any", o } };
            var actual = dict.GetValue("any", -1);
            actual.Should().Be(10);
        }

        [Fact]
        public void ShouldGetDefaultValueCorrectlyWhenExpectingList()
        {
            var dict = new Dictionary<string, object>();
            var defaultValue = new List<int>();
            var actual = dict.GetValue("any", defaultValue);
            actual.Should().BeOfType<List<int>>();
            actual.Should().BeEmpty();
        }

        [Fact]
        public void ShouldGetValueCorrectlyWhenExpectingList()
        {
            var dict = new Dictionary<string, object> { { "any", new List<object> { 11 } } };
            var actual = dict.GetValue("any", new List<object>()).Cast<int>().ToList();
            actual.Should().BeOfType<List<int>>();
            actual.Should().ContainInOrder(11);
        }

        [Fact]
        public void ShouldGetDefaultValueCorrectlyWhenExpectingMap()
        {
            var dict = new Dictionary<string, object>();
            var defaultValue = new Dictionary<string, int>();
            var actual = dict.GetValue("any", defaultValue);
            actual.Should().BeOfType<Dictionary<string, int>>();
            actual.Should().BeEmpty();
        }

        [Fact]
        public void ShouldGetValueCorrectlyWhenExpectingMap()
        {
            var dict = new Dictionary<string, object>
            {
                { "any", new Dictionary<string, object> { { "lala", 1 } } }
            };

            var actual = dict.GetValue("any", new Dictionary<string, object>());
            actual.Should().BeOfType<Dictionary<string, object>>();
            actual.GetValue("lala", 0).Should().Be(1);
        }
    }

    public class ToDictionaryMethod
    {
        [Fact]
        public void ShouldReturnNullGivenNull()
        {
            var dict = CollectionExtensions.ToDictionary(null);

            dict.Should().BeNull();
        }

        [Theory]
        [InlineData((sbyte)0)]
        [InlineData((byte)0)]
        [InlineData((short)0)]
        [InlineData((ushort)0)]
        [InlineData(0)]
        [InlineData((uint)0)]
        [InlineData((long)0)]
        [InlineData((ulong)0)]
        [InlineData((char)0)]
        [InlineData((float)0)]
        [InlineData((double)0)]
        [InlineData(true)]
        public void ShouldHandleSimpleTypes(object value)
        {
            var dict = new
            {
                key = value
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("key");
            dict.Should().ContainValue(value);
        }

        [Fact]
        public void ShouldHandleString()
        {
            var dict = new
            {
                key = "value"
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("key");
            dict.Should().ContainValue("value");
        }

        [Fact]
        public void ShouldHandleArray()
        {
            var array = new byte[2];

            var dict = new
            {
                key = array
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("key");
            dict.Should().ContainValue(array);
        }

        [Fact]
        public void ShouldHandleAnonymousObjects()
        {
            var dict = new { key1 = "value1", key2 = "value2" }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(2);
            dict.Should()
                .Contain(
                    new KeyValuePair<string, object>("key1", "value1"),
                    new KeyValuePair<string, object>("key2", "value2"));
        }

        [Fact]
        public void ShouldHandlePoco()
        {
            var dict = new MyPOCO { Key1 = "value1", Key2 = "value2" }.ToDictionary();

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
            var dict = new
            {
                InnerObject = new { Key1 = 1, Key2 = "a", Key3 = 0L }
            }.ToDictionary();

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
            var dict = new
            {
                InnerDictionary = new Dictionary<string, object>
                {
                    { "Key1", 1 },
                    { "Key2", "a" },
                    { "Key3", 0L }
                }
            }.ToDictionary();

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
            var dict = new
            {
                InnerCollection = new List<int> { 1, 2, 3 }
            }.ToDictionary();

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
            var dict = new
            {
                InnerCollection = new List<object>
                {
                    new { a = "a" },
                    3,
                    new MyPOCO { Key1 = "value1" }
                }
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("InnerCollection");

            var innerCollectionObject = dict["InnerCollection"];
            innerCollectionObject.Should().NotBeNull();
            innerCollectionObject.Should().BeAssignableTo<IList<object>>();

            var innerCollection = (IList<object>)innerCollectionObject;
            innerCollection.Should().HaveCount(3);
            innerCollection.Should()
                .Contain(
                    o => o is IDictionary<string, object> &&
                        ((IDictionary<string, object>)o).Contains(new KeyValuePair<string, object>("a", "a")));

            innerCollection.Should().Contain(3);
            innerCollection.Should()
                .Contain(
                    o => o is IDictionary<string, object> &&
                        ((IDictionary<string, object>)o).Contains(new KeyValuePair<string, object>("Key1", "value1")));
        }

        [Fact]
        public void ShouldHandleDictionaryOfArbitraryObjects()
        {
            var dict = new
            {
                InnerDictionary = new Dictionary<string, object>
                {
                    { "a", new { a = "a" } },
                    { "b", "b" },
                    { "c", 3 }
                }
            }.ToDictionary();

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
            var ex = Record.Exception(
                () => new
                {
                    InnerDictionary = new Dictionary<int, object>
                    {
                        { 1, new { a = "a" } },
                        { 2, "b" },
                        { 3, 3 }
                    }
                }.ToDictionary());

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();
            ex.Message.Should().Contain("string keys");
        }

        [Fact]
        public void ShouldHandleListOfArbitraryObjects()
        {
            var dict = new
            {
                InnerList = new List<object>
                {
                    new { a = "a" },
                    "b",
                    3
                }
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("InnerList");

            var innerListObject = dict["InnerList"];
            innerListObject.Should().NotBeNull();
            innerListObject.Should().BeAssignableTo<IList<object>>();

            var innerList = (IList<object>)innerListObject;
            innerList.Should().HaveCount(3);
            innerList[0].Should().BeAssignableTo<IDictionary<string, object>>();
            innerList[0]
                .As<IDictionary<string, object>>()
                .Should()
                .Contain(new KeyValuePair<string, object>("a", "a"));

            innerList[1].Should().Be("b");
            innerList[2].As<int>().Should().Be(3);
        }

        [Fact]
        public void ShouldHandleEnumerable()
        {
            var array = new[] { 1, 2, 3 };
            var value = new MyCollection<int>(array);

            var dict = new
            {
                key = value
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("key");
            var s = dict["key"].ToContentString();
            s.Should().Be("[1, 2, 3]"); // GetEnumerator rather than the Name field
        }

        [Fact]
        public void ShouldHandleEnumerableofEnumerable()
        {
            var array = new[] { 1, 2, 3 };
            IEnumerable element = new MyCollection<int>(array);
            var value = new MyCollection<object>(new[] { element, "a" });

            var dict = new
            {
                key = value
            }.ToDictionary();

            dict.Should().NotBeNull();
            dict.Should().HaveCount(1);
            dict.Should().ContainKey("key");
            var s = dict["key"].ToContentString();
            s.Should().Be("[[1, 2, 3], a]"); // GetEnumerator rather than the Name field
        }

        private class MyPOCO
        {
            public string Key1 { get; set; }

            public string Key2 { get; set; }
        }
    }

    public class MyCollection<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _values;

        public MyCollection(IEnumerable<T> values)
        {
            _values = values;
        }

        public string Name => "My Collection implements IEnumerable<T>";

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
