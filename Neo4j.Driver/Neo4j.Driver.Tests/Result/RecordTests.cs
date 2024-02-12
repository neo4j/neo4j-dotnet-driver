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
    private Record _record;
    private Dictionary<string, int> _fieldLookup;
    private Dictionary<string, int> _invariantFieldLookup;
    private object[] _fieldValues;

    public RecordTests()
    {
        _fieldLookup = new Dictionary<string, int> { { "Key1", 0 }, { "Key2", 1 } };
        _invariantFieldLookup = new Dictionary<string, int>(_fieldLookup, StringComparer.InvariantCultureIgnoreCase);
        _fieldValues = new object[] { "Value1", "Value2" };
        _record = new Record(_fieldLookup, _invariantFieldLookup, _fieldValues);
    }

    [Fact]
    public void GetValueByCaseInsensitiveKey_ReturnsCorrectValue()
    {
        _record.GetValueByCaseInsensitiveKey("KEY1").Should().Be("Value1");
        _record.GetValueByCaseInsensitiveKey("KEY2").Should().Be("Value2");
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
