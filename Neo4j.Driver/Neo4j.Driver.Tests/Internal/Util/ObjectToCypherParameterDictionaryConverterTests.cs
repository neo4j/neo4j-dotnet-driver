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
using System.Reflection;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Mapping;
using Neo4j.Driver.Internal.Util;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Mapping.ConventionTranslation;
using Neo4j.Driver.Tests.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Util;

public class ObjectToCypherParameterDictionaryConverterTests : MappingTestWithGlobalState
{
    private readonly AutoMocker _mocker = new();

    private ObjectToCypherParameterDictionaryConverter GetSubject() =>
        _mocker.CreateInstance<ObjectToCypherParameterDictionaryConverter>();

    private void SetupDefaultMocks()
    {
        _mocker.GetMock<IMappingBindingProvider>()
            .Setup(p => p.GetMappingBinding(It.IsAny<PropertyInfo>()))
            .Returns<PropertyInfo>(p => new MappingBinding(p.Name, EntityMappingSource.Property));

        _mocker.GetMock<ICypherParameterValueTransformer>()
            .Setup(t => t.Transform(
                It.IsAny<object>(),
                It.IsAny<Func<object, IDictionary<string, object>, IDictionary<string, object>>>()))
            .Returns<object, Func<object, IDictionary<string, object>, IDictionary<string, object>>>((v, _) => v);
    }

    private void SetupLowercaseCypherParameterTranslation(bool translateCypherParameters = false)
    {
        SetupDefaultMocks();
        _mocker.GetMock<IConventionTranslator>()
            .Setup(t => t.Translate(It.IsAny<string>()))
            .Returns<string>(s => s.ToLowerInvariant());

        ((IRecordObjectMapping)RecordObjectMapping.Instance)
            .TranslateIdentifiers(_mocker.Get<IConventionTranslator>(), translateCypherParameters);
    }

    [Fact]
    public void ShouldReturnNullGivenNull()
    {
        var dict = GetSubject().Convert(null);
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
        SetupDefaultMocks();
        var dict = GetSubject().Convert(new { key = value });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        dict.Should().ContainValue(value);
    }

    [Fact]
    public void ShouldHandleString()
    {
        SetupDefaultMocks();
        var dict = GetSubject().Convert(new { key = "value" });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        dict.Should().ContainValue("value");
    }

    [Fact]
    public void ShouldHandleArray()
    {
        SetupDefaultMocks();
        var array = new byte[2];
        var dict = GetSubject().Convert(new { key = array });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        dict.Should().ContainValue(array);
    }

    [Fact]
    public void ShouldHandleAnonymousObjects()
    {
        SetupDefaultMocks();
        var dict = GetSubject().Convert(new { key1 = "value1", key2 = "value2" });
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
        SetupDefaultMocks();
        var vector = Vector.Create([1.0, 2.0, 3.0]);
        var dict = GetSubject().Convert(new { vector });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("vector");
        dict["vector"].Should().BeOfType<Vector<double>>();
        ((Vector<double>)dict["vector"]).Values.Should().BeEquivalentTo([1.0, 2.0, 3.0]);
    }

    [Fact]
    public void ShouldHandlePoco()
    {
        SetupDefaultMocks();
        var dict = GetSubject().Convert(new MyPoco { Key1 = "value1", Key2 = "value2" });
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
        SetupDefaultMocks();
        var inner = new { Key1 = 1, Key2 = "a", Key3 = 0L };
        var dict = GetSubject().Convert(new { InnerObject = inner });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerObject");
        dict["InnerObject"].Should().BeSameAs(inner);
    }

    [Fact]
    public void ShouldHandleDictionary()
    {
        SetupDefaultMocks();
        var dict = GetSubject()
            .Convert(
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
        SetupDefaultMocks();
        var dict = GetSubject().Convert(new { InnerCollection = new List<int> { 1, 2, 3 } });
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
        SetupDefaultMocks();
        var anon = new { a = "a" };
        var poco = new MyPoco { Key1 = "value1" };
        var dict = GetSubject()
            .Convert(new { InnerCollection = new List<object> { anon, 3, poco } });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerCollection");
        var innerCollection = (List<object>)dict["InnerCollection"];
        innerCollection.Should().HaveCount(3);
        innerCollection[0].Should().BeSameAs(anon);
        innerCollection[1].Should().Be(3);
        innerCollection[2].Should().BeSameAs(poco);
    }

    [Fact]
    public void ShouldHandleDictionaryOfArbitraryObjects()
    {
        SetupDefaultMocks();
        var anon = new { a = "a" };
        var dict = GetSubject()
            .Convert(
                new
                {
                    InnerDictionary = new Dictionary<string, object>
                    {
                        { "a", anon },
                        { "b", "b" },
                        { "c", 3 }
                    }
                });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerDictionary");
        var innerDictionary = (IDictionary<string, object>)dict["InnerDictionary"];
        innerDictionary.Should().HaveCount(3);
        innerDictionary["a"].Should().BeSameAs(anon);
        innerDictionary.Should().Contain(new KeyValuePair<string, object>("b", "b"));
        innerDictionary.Should().Contain(new KeyValuePair<string, object>("c", 3));
    }

    [Fact]
    public void ShouldRaiseExceptionWhenDictionaryKeysAreNotStrings()
    {
        SetupDefaultMocks();
        _mocker.GetMock<ICypherParameterValueTransformer>()
            .Setup(t => t.Transform(
                It.Is<object>(v => v is Dictionary<int, object>),
                It.IsAny<Func<object, IDictionary<string, object>, IDictionary<string, object>>>()))
            .Throws(new InvalidOperationException("dictionaries passed as part of a parameter to cypher queries should have string keys!"));

        var ex = Record.Exception(() => GetSubject()
            .Convert(
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
        SetupDefaultMocks();
        var anon = new { a = "a" };
        var dict = GetSubject()
            .Convert(new { InnerList = new List<object> { anon, "b", 3 } });

        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("InnerList");
        var innerList = (List<object>)dict["InnerList"];
        innerList.Should().HaveCount(3);
        innerList[0].Should().BeSameAs(anon);
        innerList[1].Should().Be("b");
        innerList[2].Should().Be(3);
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
        var result = GetSubject().Convert(emptyDictionary);
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

        var result = GetSubject().Convert(sourceDictionary);
        result.Should().HaveCount(2);
        result["Key1"].Should().BeEquivalentTo(sourceDictionary["Key1"]);
        result["Key2"].Should().BeEquivalentTo(sourceDictionary["Key2"]);
    }

    [Fact]
    public void ToDictionary_ShouldReturnNullForNullDictionary()
    {
        Dictionary<string, Person> nullDictionary = null;
        var actual = GetSubject().Convert(nullDictionary);
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

        var result = GetSubject().Convert(nestedDictionary);
        result.Should().ContainKey("Nested");
        var innerDict = result["Nested"].As<Dictionary<string, Person>>();
        innerDict.Should().ContainKey("InnerKey");
        innerDict["InnerKey"].Should().BeEquivalentTo(new Person { Name = "Doe", Age = 40 });
    }

    [Fact]
    public void ShouldHandleEnumerable()
    {
        SetupDefaultMocks();
        var array = new[] { 1, 2, 3 };
        var value = new MyCollection<int>(array);
        var dict = GetSubject().Convert(new { key = value });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        var s = dict["key"].ToContentString();
        s.Should().Be("[1, 2, 3]");
    }

    [Fact]
    public void ShouldHandleEnumerableOfEnumerable()
    {
        SetupDefaultMocks();
        var array = new[] { 1, 2, 3 };
        IEnumerable element = new MyCollection<int>(array);
        var value = new MyCollection<object>(new[] { element, "a" });
        var dict = GetSubject().Convert(new { key = value });
        dict.Should().NotBeNull();
        dict.Should().HaveCount(1);
        dict.Should().ContainKey("key");
        var s = dict["key"].ToContentString();
        s.Should().Be("[[1, 2, 3], a]");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldObserveMappingBindingsAttribute(bool translateCypherParameters)
    {
        SetupDefaultMocks();
        RecordObjectMapping.TranslateIdentifiers(translateCypherParameters);
        _mocker.GetMock<IMappingBindingProvider>()
            .Setup(p => p.GetMappingBinding(It.Is<PropertyInfo>(pi => pi.Name == "MappingBindingsDecorated")))
            .Returns(new MappingBinding("MappingBindingsDecorated", EntityMappingSource.Property) { CypherParameterName = "decorated_property_with_bindings" });

        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass { MappingBindingsDecorated = propertyValue };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("decorated_property_with_bindings");
        parameters["decorated_property_with_bindings"].Should().Be(propertyValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldObserveCypherParameterMappingAttribute(bool translateCypherParameters)
    {
        SetupDefaultMocks();
        RecordObjectMapping.TranslateIdentifiers(translateCypherParameters);
        _mocker.GetMock<IMappingBindingProvider>()
            .Setup(p => p.GetMappingBinding(It.Is<PropertyInfo>(pi => pi.Name == "SomeProperty")))
            .Returns(new MappingBinding("SomeProperty", EntityMappingSource.Property) { CypherParameterName = "explicitParamName" });

        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass { SomeProperty = propertyValue };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("explicitParamName");
        parameters["explicitParamName"].Should().Be(propertyValue);
    }

    [Fact]
    public void ShouldNotTranslateParametersByDefault()
    {
        SetupLowercaseCypherParameterTranslation();
        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass
        {
            NotDecoratedProperty = propertyValue,
        };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("NotDecoratedProperty");
        parameters["NotDecoratedProperty"].Should().Be(propertyValue);
    }

    [Fact]
    public void ShouldNotTranslateParametersWhenToldNotTo()
    {
        SetupLowercaseCypherParameterTranslation(false);
        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass
        {
            NotDecoratedProperty = propertyValue,
        };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("NotDecoratedProperty");
        parameters["NotDecoratedProperty"].Should().Be(propertyValue);
    }

    [Fact]
    public void ShouldTranslateParameters()
    {
        SetupLowercaseCypherParameterTranslation(true);
        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass
        {
            NotDecoratedProperty = propertyValue,
        };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("notdecoratedproperty");
        parameters["notdecoratedproperty"].Should().Be(propertyValue);
    }

    [Fact]
    public void ShouldTranslateParametersInNestedObjects()
    {
        SetupLowercaseCypherParameterTranslation(true);
        var innerPoco = new MyPoco { Key1 = "value1", Key2 = "value2" };

        var parameters = GetSubject().Convert(new { InnerObject = innerPoco });

        parameters.Should().ContainKey("innerobject");
        parameters["innerobject"].Should().BeSameAs(innerPoco);
    }

    [Fact]
    public void ShouldTranslateParametersInNestedObjectsInList()
    {
        SetupLowercaseCypherParameterTranslation(true);
        var innerPoco = new MyPoco { Key1 = "value1", Key2 = "value2" };
        var innerList = new List<MyPoco> { innerPoco };

        var parameters = GetSubject().Convert(new { InnerList = innerList });

        parameters.Should().ContainKey("innerlist");
        parameters["innerlist"].Should().BeSameAs(innerList);
    }

    [Fact]
    public void ShouldTranslateParametersInNestedObjectsInDictionary()
    {
        SetupLowercaseCypherParameterTranslation(true);
        var innerDict = new Dictionary<string, MyPoco>
        {
            ["entry"] = new() { Key1 = "value1", Key2 = "value2" }
        };

        var parameters = GetSubject().Convert(new { InnerDictionary = innerDict });

        parameters.Should().ContainKey("innerdictionary");
        parameters["innerdictionary"].Should().BeSameAs(innerDict);
    }

    [Fact]
    public void LaterAttributesShouldOverrideEarlierAttributes()
    {
        SetupDefaultMocks();
        _mocker.GetMock<IMappingBindingProvider>()
            .Setup(p => p.GetMappingBinding(It.Is<PropertyInfo>(pi => pi.Name == "MultiplyDecoratedProperty")))
            .Returns(new MappingBinding("MultiplyDecoratedProperty", EntityMappingSource.Property) { CypherParameterName = "multiply_decorated_property" });

        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass { MultiplyDecoratedProperty = propertyValue };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("multiply_decorated_property");
        parameters["multiply_decorated_property"].Should().Be(propertyValue);
    }

    [Fact]
    public void CustomCypherParameterAttributeShouldWork()
    {
        SetupDefaultMocks();
        _mocker.GetMock<IMappingBindingProvider>()
            .Setup(p => p.GetMappingBinding(It.Is<PropertyInfo>(pi => pi.Name == "CustomDecoratedProperty")))
            .Returns(new MappingBinding("CustomDecoratedProperty", EntityMappingSource.Property) { CypherParameterName = "CustomParameterName" });

        var propertyValue = Guid.NewGuid().ToString();
        var testObj = new ParameterMappingTestClass { CustomDecoratedProperty = propertyValue };

        var parameters = GetSubject().Convert(testObj);

        parameters.Should().ContainKey("CustomParameterName");
        parameters["CustomParameterName"].Should().Be(propertyValue);
    }

    private class ParameterMappingTestClass
    {
        [MappingBindings(CypherParameterName = "decorated_property_with_bindings")]
        public string MappingBindingsDecorated { get; init; }

        [CypherParameterMapping("explicitParamName")]
        public string SomeProperty { get; init; }

        public string NotDecoratedProperty { get; init; }

        [MappingSource("not_used", CypherParameterName = "shouldn't_be_used")]
        [CypherParameterMapping("multiply_decorated_property")]
        public string MultiplyDecoratedProperty { get; init; }

        [CustomCypherParameter]
        public string CustomDecoratedProperty { get; init; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    private class CustomCypherParameterAttribute : Attribute, IMappingBindingMutator
    {
        public void Mutate(MappingBinding binding)
        {
            binding.CypherParameterName = "CustomParameterName";
        }
    }

    private class MyPoco
    {
        public string Key1 { get; set; }
        public string Key2 { get; set; }
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

    [Fact]
    public void ShouldHandleRecordTypes()
    {
        SetupDefaultMocks();
        var result = GetSubject().Convert(new TestRecord { Id = "abc" });
        result.Should().HaveCount(1);
        result.Should().ContainKey("Id");
        result["Id"].Should().Be("abc");
    }

    [Fact]
    public void ShouldSkipWriteOnlyProperties()
    {
        SetupDefaultMocks();
        var result = GetSubject().Convert(new WithWriteOnlyProperty { ReadableProperty = "readable" });
        result.Should().HaveCount(1);
        result.Should().ContainKey("ReadableProperty");
        result.Should().NotContainKey("WriteOnly");
    }

    [Fact]
    public void ShouldSkipIndexerProperties()
    {
        SetupDefaultMocks();
        var result = GetSubject().Convert(new WithIndexer { Name = "test" });
        result.Should().HaveCount(1);
        result.Should().ContainKey("Name");
    }

    [Fact]
    public void ShouldSkipStaticProperties()
    {
        SetupDefaultMocks();
        var result = GetSubject().Convert(new WithStaticProperty { InstanceProperty = "instance" });
        result.Should().HaveCount(1);
        result.Should().ContainKey("InstanceProperty");
        result.Should().NotContainKey("StaticProperty");
    }

    private record TestRecord
    {
        public string Id { get; init; }
    }

    private class WithWriteOnlyProperty
    {
        public string ReadableProperty { get; set; }
        public string WriteOnly { set { } }
    }

    private class WithIndexer
    {
        private readonly Dictionary<string, string> _data = new();
        public string Name { get; set; }
        public string this[string key] { get => _data[key]; set => _data[key] = value; }
    }

    private class WithStaticProperty
    {
        public string InstanceProperty { get; set; }
        public static string StaticProperty => "static";
    }
}
