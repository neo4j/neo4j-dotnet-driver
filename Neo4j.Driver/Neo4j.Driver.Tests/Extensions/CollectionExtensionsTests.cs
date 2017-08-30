using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using CollectionExtensions = Neo4j.Driver.Internal.CollectionExtensions;

namespace Neo4j.Driver.Tests.Extensions
{
    public class CollectionExtensionsTests
    {

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
            [InlineData((int)0)]
            [InlineData((uint)0)]
            [InlineData((long)0)]
            [InlineData((ulong)0)]
            [InlineData((char)0)]
            [InlineData((float)0)]
            [InlineData((double)0)]
            [InlineData(true)]
            public void ShouldHandleSimpleTypes(object value)
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    key = value
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("key");
                dict.Should().ContainValue(value);
            }

            [Fact]
            public void ShouldHandleString()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    key = "value"
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("key");
                dict.Should().ContainValue("value");
            }

            [Fact]
            public void ShouldHandleArray()
            {
                var array = new byte[2];

                var dict = CollectionExtensions.ToDictionary(new
                {
                    key = array
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("key");
                dict.Should().ContainValue(array);
            }

            [Fact]
            public void ShouldHandleAnonymousObjects()
            {
                var dict = CollectionExtensions.ToDictionary(new { key1 = "value1", key2 = "value2" });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(2);
                dict.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("key1", "value1"),
                    new KeyValuePair<string, object>("key2", "value2")
                });
            }

            [Fact]
            public void ShouldHandlePoco()
            {
                var dict = CollectionExtensions.ToDictionary(new MyPOCO() {Key1 = "value1", Key2 = "value2"});

                dict.Should().NotBeNull();
                dict.Should().HaveCount(2);
                dict.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Key1", "value1"),
                    new KeyValuePair<string, object>("Key2", "value2")
                });
            }

            [Fact]
            public void ShouldHandleDeeperObjects()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    InnerObject = new {Key1 = 1, Key2 = "a", Key3 = 0L}
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("InnerObject");

                var innerObjectObject = dict["InnerObject"];
                innerObjectObject.Should().NotBeNull();
                innerObjectObject.Should().BeAssignableTo<IDictionary<string, object>>();

                var innerObject = (IDictionary<string, object>) innerObjectObject;
                innerObject.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Key1", 1),
                    new KeyValuePair<string, object>("Key2", "a"),
                    new KeyValuePair<string, object>("Key3", 0L)
                });
            }

            [Fact]
            public void ShouldHandleDictionary()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    InnerDictionary = new Dictionary<string, object>()
                    {
                        {"Key1", 1},
                        {"Key2", "a"},
                        {"Key3", 0L}
                    }
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("InnerDictionary");

                var innerDictionaryObject = dict["InnerDictionary"];
                innerDictionaryObject.Should().NotBeNull();
                innerDictionaryObject.Should().BeAssignableTo<IDictionary<string, object>>();

                var innerDictionary = (IDictionary<string, object>)innerDictionaryObject;
                innerDictionary.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Key1", 1),
                    new KeyValuePair<string, object>("Key2", "a"),
                    new KeyValuePair<string, object>("Key3", 0L)
                });
            }

            [Fact]
            public void ShouldHandleCollections()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    InnerCollection = new List<int>() {1, 2, 3}
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("InnerCollection");

                var innerCollectionObject = dict["InnerCollection"];
                innerCollectionObject.Should().NotBeNull();
                innerCollectionObject.Should().BeAssignableTo<IList<int>>();

                var innerCollection = (IList<int>)innerCollectionObject;
                innerCollection.Should().Contain(new[] {1, 2, 3});
            }

            [Fact]
            public void ShouldHandleCollectionsOfArbitraryObjects()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    InnerCollection = new List<object>()
                    {
                        new {a = "a"},
                        3,
                        new MyPOCO() {Key1 = "value1" }
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
                innerCollection.Should().Contain(o => o is IDictionary<string, object> &&
                                                      ((IDictionary<string, object>) o).Contains(
                                                          new KeyValuePair<string, object>("a", "a")));
                innerCollection.Should().Contain(3);
                innerCollection.Should().Contain(o => o is IDictionary<string, object> &&
                                                      ((IDictionary<string, object>)o).Contains(
                                                          new KeyValuePair<string, object>("Key1", "value1")));
            }

            [Fact]
            public void ShouldHandleDictionaryOfArbitraryObjects()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    InnerDictionary = new Dictionary<string, object>()
                    {
                        {"a", new {a = "a"}},
                        {"b", "b"},
                        {"c", 3}
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
                innerDictionary["a"].As<IDictionary<string, object>>().Should().Contain(new KeyValuePair<string, object>("a", "a"));
                innerDictionary.Should().Contain(new KeyValuePair<string, object>("b", "b"));
                innerDictionary.Should().Contain(new KeyValuePair<string, object>("c", 3));
            }

            [Fact]
            public void ShouldRaiseExceptionWhenDictionaryKeysAreNotStrings()
            {
                var ex = Record.Exception(() => CollectionExtensions.ToDictionary(new
                {
                    InnerDictionary = new Dictionary<int, object>()
                    {
                        {1, new {a = "a"}},
                        {2, "b"},
                        {3, 3}
                    }
                }));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<InvalidOperationException>();
                ex.Message.Should().Contain("string keys");
            }

            private class MyPOCO
            {
                public string Key1 { get; set; }

                public string Key2 { get; set; }
            }
        }

    }
}
