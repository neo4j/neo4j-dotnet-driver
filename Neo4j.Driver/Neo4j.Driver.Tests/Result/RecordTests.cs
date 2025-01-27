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
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Result;

public class RecordTests
{
    private readonly Record _record;

    public RecordTests()
    {
        var fieldLookup = new Dictionary<string, int> { { "Key1", 0 }, { "Key2", 1 } };
        var invariantFieldLookup = new Dictionary<string, int>(fieldLookup, StringComparer.InvariantCultureIgnoreCase);
        var fieldValues = new object[] { "Value1", "Value2" };
        _record = new Record(fieldLookup, invariantFieldLookup, fieldValues);
    }

    [Fact]
    public void Indexer_IntParameter_ReturnsCorrectValue()
    {
        _record[0].Should().Be("Value1");
        _record[1].Should().Be("Value2");
    }

    [Fact]
    public void Indexer_StringParameter_ReturnsCorrectValue()
    {
        _record["Key1"].Should().Be("Value1");
        _record["Key2"].Should().Be("Value2");
    }

    [Fact]
    public void Indexer_StringParameter_IsCaseSensitive()
    {
        var act = () => _record["KEY1"];
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Values_ReturnsCorrectDictionary()
    {
        var expectedDictionary = new Dictionary<string, object> { { "Key1", "Value1" }, { "Key2", "Value2" } };
        _record.Values.Should().Equal(expectedDictionary);
    }

    [Fact]
    public void Keys_ReturnsCorrectList()
    {
        var expectedKeys = new List<string> { "Key1", "Key2" };
        _record.Keys.Should().Equal(expectedKeys);
    }

    [Fact]
    public void Get_IsCaseSensitive()
    {
        _record.Get<string>("Key1").Should().Be("Value1");
        _record.Invoking(r => r.Get<string>("KEY1")).Should().Throw<KeyNotFoundException>();
        _record.Get<string>("Key2").Should().Be("Value2");
        _record.Invoking(r => r.Get<string>("KEY2")).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGet_IsCaseSensitive()
    {
        _record.TryGet<string>("Key1", out var value).Should().BeTrue();
        value.Should().Be("Value1");
        _record.TryGet<string>("KEY1", out var _).Should().BeFalse();
        _record.TryGet("Key2", out value).Should().BeTrue();
        value.Should().Be("Value2");
        _record.TryGet<string>("KEY2", out var _).Should().BeFalse();
    }

    [Fact]
    public void GetCaseInsensitive_IsCaseInsensitive()
    {
        _record.GetCaseInsensitive<string>("Key1").Should().Be("Value1");
        _record.GetCaseInsensitive<string>("KEY1").Should().Be("Value1");
        _record.GetCaseInsensitive<string>("Key2").Should().Be("Value2");
        _record.GetCaseInsensitive<string>("KEY2").Should().Be("Value2");
    }

    [Fact]
    public void TryGetCaseInsensitive_IsCaseInsensitive()
    {
        _record.TryGetCaseInsensitive<string>("Key1", out var value).Should().BeTrue();
        value.Should().Be("Value1");
        _record.TryGetCaseInsensitive("KEY1", out value).Should().BeTrue();
        value.Should().Be("Value1");
        _record.TryGetCaseInsensitive("Key2", out value).Should().BeTrue();
        value.Should().Be("Value2");
        _record.TryGetCaseInsensitive("KEY2", out value).Should().BeTrue();
        value.Should().Be("Value2");
    }

    [Fact]
    public void TryGet_WithNonExistentKey_ReturnsFalse()
    {
        _record.TryGet<string>("nonexistent", out var _).Should().BeFalse();
    }

    [Fact]
    public void TryGetCaseInsensitive_WithNonExistentKey_ReturnsFalse()
    {
        _record.TryGetCaseInsensitive<string>("nonexistent", out var _).Should().BeFalse();
    }

    [Fact]
    public void GetEnumerator_ReturnsCorrectEnumerator()
    {
        var enumerable = _record as IEnumerable<KeyValuePair<string, object>>;
        using var enumerator = enumerable.GetEnumerator();
        enumerator.MoveNext();
        enumerator.Current.Should().Be(new KeyValuePair<string, object>("Key1", "Value1"));
        enumerator.MoveNext();
        enumerator.Current.Should().Be(new KeyValuePair<string, object>("Key2", "Value2"));
        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void GetEnumerator_EnumeratesCorrectly()
    {
        var enumerable = _record as IEnumerable<KeyValuePair<string, object>>;
        var expected = new List<KeyValuePair<string, object>>
        {
            new("Key1", "Value1"),
            new("Key2", "Value2")
        };

        enumerable.Should().Equal(expected);
    }

    [Fact]
    public void ContainsKey_ReturnsCorrectResult()
    {
        var dictionary = _record as IReadOnlyDictionary<string, object>;
        dictionary.ContainsKey("Key1").Should().BeTrue();
        dictionary.ContainsKey("NonExistentKey").Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_ReturnsCorrectResult()
    {
        var dictionary = _record as IReadOnlyDictionary<string, object>;
        dictionary.TryGetValue("Key1", out var value).Should().BeTrue();
        value.Should().Be("Value1");
        dictionary.TryGetValue("NonExistentKey", out var _).Should().BeFalse();
    }

    [Fact]
    public void Keys_ReturnsCorrectKeys()
    {
        var dictionary = _record as IReadOnlyDictionary<string, object>;
        var expectedKeys = new List<string> { "Key1", "Key2" };
        dictionary.Keys.Should().Equal(expectedKeys);
    }

    [Fact]
    public void Values_ReturnsCorrectValues()
    {
        var dictionary = _record as IReadOnlyDictionary<string, object>;
        dictionary.Values.Should().Equal("Value1", "Value2");
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        var dictionary = _record as IReadOnlyDictionary<string, object>;
        dictionary.Count.Should().Be(2);
    }
}
