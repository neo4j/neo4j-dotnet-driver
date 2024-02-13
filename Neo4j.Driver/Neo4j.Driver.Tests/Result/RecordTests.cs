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
    public void TryGetValueByCaseInsensitiveKey_ReturnsCorrectValue()
    {
        _record.TryGetValueByCaseInsensitiveKey("KEY1", out var value).Should().BeTrue();
        value.Should().Be("Value1");

        _record.TryGetValueByCaseInsensitiveKey("KEY2", out value).Should().BeTrue();
        value.Should().Be("Value2");

        _record.TryGetValueByCaseInsensitiveKey("KEY3", out value).Should().BeFalse();
        value.Should().BeNull();
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
}
