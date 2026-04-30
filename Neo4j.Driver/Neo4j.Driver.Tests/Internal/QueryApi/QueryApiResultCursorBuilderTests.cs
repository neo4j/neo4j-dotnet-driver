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

#nullable enable

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiResultCursorBuilderTests
{
    private static readonly Query AnyQuery = new("RETURN 1");

    private static readonly IQueryApiResultCursorBuilder Builder = new QueryApiResultCursorBuilder(
        new QueryApiResultSummaryFactory(new Mock<IServerInfo>().Object, "neo4j"));

    /// <summary>
    /// Builds a response with a single row and single column, then fetches that record.
    /// </summary>
    private static async Task<IRecord> FetchSingle(object? value)
    {
        var element = JsonSerializer.SerializeToElement(value);
        var response = new QueryApiResponse
        {
            Fields = ["v"],
            Rows = [[element]]
        };

        var cursor = Builder.Build(response, AnyQuery);
        await cursor.FetchAsync();
        return cursor.Current;
    }

    // --- Field layout ---

    [Fact]
    public async Task Build_MapsFieldsToCorrectColumns()
    {
        var response = new QueryApiResponse
        {
            Fields = ["name", "age"],
            Rows = [[JsonSerializer.SerializeToElement("Alice"), JsonSerializer.SerializeToElement(30)]]
        };

        var cursor = Builder.Build(response, AnyQuery);
        await cursor.FetchAsync();

        cursor.Current["name"].Should().Be("Alice");
        cursor.Current["age"].Should().Be(30L);
    }

    [Fact]
    public async Task Build_EmptyResponse_ProducesNoCursor()
    {
        var response = new QueryApiResponse { Fields = ["x"], Rows = [] };
        var cursor = Builder.Build(response, AnyQuery);
        (await cursor.FetchAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task KeysAsync_ReturnsResponseFields()
    {
        var response = new QueryApiResponse { Fields = ["a", "b", "c"], Rows = [] };
        var cursor = Builder.Build(response, AnyQuery);
        (await cursor.KeysAsync()).Should().Equal("a", "b", "c");
    }

    // --- JsonElement conversion ---

    [Fact]
    public async Task Converts_StringValue()
    {
        var record = await FetchSingle("hello");
        record["v"].Should().Be("hello");
    }

    [Fact]
    public async Task Converts_IntegerValue_AsLong()
    {
        var record = await FetchSingle(long.MaxValue);
        record["v"].Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task Converts_SmallInteger_AsLong()
    {
        var record = await FetchSingle(42);
        record["v"].Should().Be(42L);
    }

    [Fact]
    public async Task Converts_DoubleValue()
    {
        var record = await FetchSingle(3.14);
        record["v"].Should().Be(3.14);
    }

    [Fact]
    public async Task Converts_BoolTrue()
    {
        var record = await FetchSingle(true);
        record["v"].Should().Be(true);
    }

    [Fact]
    public async Task Converts_BoolFalse()
    {
        var record = await FetchSingle(false);
        record["v"].Should().Be(false);
    }

    [Fact]
    public async Task Converts_NullValue()
    {
        var record = await FetchSingle(null);
        record["v"].Should().BeNull();
    }

    [Fact]
    public async Task Converts_ArrayValue_ToList()
    {
        var record = await FetchSingle(new[] { 1, 2, 3 });
        record["v"].Should().BeEquivalentTo(new List<object?> { 1L, 2L, 3L });
    }

    [Fact]
    public async Task Converts_NestedArray()
    {
        var record = await FetchSingle(new[] { new[] { 1, 2 }, new[] { 3, 4 } });
        var outer = record["v"].Should().BeAssignableTo<List<object?>>().Subject;
        outer[0].Should().BeEquivalentTo(new List<object?> { 1L, 2L });
        outer[1].Should().BeEquivalentTo(new List<object?> { 3L, 4L });
    }

    [Fact]
    public async Task Converts_PlainObject_ToDictionary()
    {
        var record = await FetchSingle(new { x = 1, y = 2 });
        var dict = record["v"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        dict["x"].Should().Be(1L);
        dict["y"].Should().Be(2L);
    }

    [Fact]
    public async Task UnsupportedTypedValue_ReturnsStringPlaceholder()
    {
        var typedValue = new Dictionary<string, object> { ["$type"] = "Node", ["_value"] = new { } };
        var record = await FetchSingle(typedValue);
        record["v"].Should().Be("Unsupported type: Node");
    }
}
