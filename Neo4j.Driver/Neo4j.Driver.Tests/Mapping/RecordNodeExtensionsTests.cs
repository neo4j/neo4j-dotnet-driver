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
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Preview.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class RecordNodeExtensionsTests
{
    [Fact]
    public void ShouldGetTypedValueFromRecord_string()
    {
        var record = TestRecord.Create(new[] { "key" }, new object[] { "value" });
        record.GetString("key").Should().Be("value");
    }

    [Fact]
    public void ShouldGetTypedValueFromRecord_int()
    {
        var record = TestRecord.Create(new[] { "key" }, new object[] { 1L });
        record.GetInt("key").Should().Be(1);
    }

    [Fact]
    public void ShouldGetTypedValueFromRecord_long()
    {
        var record = TestRecord.Create(new[] { "key" }, new object[] { 1L });
        record.GetLong("key").Should().Be(1L);
    }

    [Fact]
    public void ShouldGetTypedValueFromRecord_double()
    {
        var record = TestRecord.Create(new[] { "key" }, new object[] { 1.0 });
        record.GetDouble("key").Should().Be(1.0);
    }

    [Fact]
    public void ShouldGetTypedValueFromRecord_float()
    {
        var record = TestRecord.Create(new[] { "key" }, new object[] { 1.0 });
        record.GetFloat("key").Should().Be(1.0f);
    }

    [Fact]
    public void ShouldGetTypedValueFromRecord_bool()
    {
        var record = TestRecord.Create(new[] { "key" }, new object[] { true });
        record.GetBool("key").Should().Be(true);
    }

    [Fact]
    public void ShouldGetTypedValueFromRecord_entity()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", "value" } });
        var record = TestRecord.Create(new[] { "key" }, new object[] { node });
        record.GetEntity("key").Should().Be(node);
    }

    [Fact]
    public void ShouldGetValueFromEntity_string()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", "value" } });
        node.GetString("key").Should().Be("value");
    }

    [Fact]
    public void ShouldGetValueFromEntity_int()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", 1L } });
        node.GetInt("key").Should().Be(1);
    }

    [Fact]
    public void ShouldGetValueFromEntity_long()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", 1L } });
        node.GetLong("key").Should().Be(1L);
    }

    [Fact]
    public void ShouldGetValueFromEntity_double()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", 1.0 } });
        node.GetDouble("key").Should().Be(1.0);
    }

    [Fact]
    public void ShouldGetValueFromEntity_float()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", 1.0 } });
        node.GetFloat("key").Should().Be(1.0f);
    }

    [Fact]
    public void ShouldGetValueFromEntity_bool()
    {
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "key", true } });
        node.GetBool("key").Should().Be(true);
    }

    [Fact]
    public void ShouldGetValueFromEntity_Dictionary()
    {
        var dictionary = new Dictionary<string, object> { { "key", "value" } };
        var node = new Node(1, new[] { "Node" }, new Dictionary<string, object> { { "field", dictionary } });
        node.GetValue<Dictionary<string, object>>("field").Should().BeEquivalentTo(dictionary);
    }
}
