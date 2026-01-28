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

namespace Neo4j.Driver.Tests.Internal.Util;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Util;
using Xunit;

public class CypherParameterValueTransformerTests
{
    private readonly CypherParameterValueTransformer _transformer = new();

    [Fact]
    public void Transform_Null_ReturnsNull()
    {
        _transformer.Transform(null).Should().BeNull();
    }

    [Fact]
    public void Transform_String_ReturnsSameString()
    {
        var input = "hello";
        var result = _transformer.Transform(input);
        result.Should().Be(input);
    }

    [Fact]
    public void Transform_IntArray_ReturnsIntArray()
    {
        var input = new[] { 1, 2, 3 };
        var result = _transformer.Transform(input);
        result.Should()
            .BeOfType<int[]>()
            .Which.Should()
            .BeEquivalentTo(1, 2, 3);
    }

    [Fact]
    public void Transform_ListOfStrings_ReturnsListOfStrings()
    {
        var input = new List<string> { "a", "b" };
        var result = _transformer.Transform(input);
        result.Should()
            .BeOfType<List<string>>()
            .Which.Should()
            .BeEquivalentTo("a", "b");
    }

    [Fact]
    public void Transform_DictionaryStringInt_ReturnsDictionaryStringObject()
    {
        var input = new Dictionary<string, int> { { "x", 1 }, { "y", 2 } };
        var result = _transformer.Transform(input);
        result.Should()
            .BeOfType<Dictionary<string, object>>()
            .Which.Should()
            .BeEquivalentTo(new Dictionary<string, object> { { "x", 1 }, { "y", 2 } });
    }

    [Fact]
    public void Transform_DictionaryWithNonStringKey_Throws()
    {
        var input = new Dictionary<int, string> { { 1, "a" } };
        Action act = () => _transformer.Transform(input);
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*string keys*");
    }

    private class TestObject
    {
        public int A { get; set; }
        public string B { get; set; }
    }

    [Fact]
    public void Transform_Object_ReturnsDictionaryOfProperties()
    {
        var input = new TestObject { A = 42, B = "foo" };
        var result = _transformer.Transform(input);
        result.Should()
            .BeOfType<Dictionary<string, object>>()
            .Which.Should()
            .Contain(new KeyValuePair<string, object>("A", 42))
            .And.Contain(new KeyValuePair<string, object>("B", "foo"));
    }

    [Fact]
    public void Transform_ListOfObjects_ReturnsListOfDictionaries()
    {
        var input = new List<TestObject>
        {
            new TestObject { A = 1, B = "x" },
            new TestObject { A = 2, B = "y" }
        };

        var result = _transformer.Transform(input);
        result.Should().BeOfType<List<object>>();
        var list = result as List<object>;
        list.Should().AllBeOfType<Dictionary<string, object>>();
    }

    [Fact]
    public void Transform_Vector_ReturnsSameVector()
    {
        var input = Vector.Create([1.0, 2.0, 3.0]);
        var result = _transformer.Transform(input);
        result.Should().BeOfType<Vector<double>>();
        ((Vector<double>) result).Values.Should().BeEquivalentTo([1.0, 2.0, 3.0]);
    }
}
