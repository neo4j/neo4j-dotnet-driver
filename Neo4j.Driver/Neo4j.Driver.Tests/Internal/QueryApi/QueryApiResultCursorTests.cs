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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiResultCursorTests
{
    private static readonly Query AnyQuery = new("RETURN 1");
    private static readonly IServerInfo AnyServer = new Mock<IServerInfo>().Object;
    private const string AnyDatabase = "neo4j";

    private static IResultCursor MakeCursor(
        string[] fields,
        object?[][] rows,
        Query? query = null,
        IServerInfo? serverInfo = null,
        string? database = null)
    {
        var response = new QueryApiResponse
        {
            Fields = fields,
            Rows = rows.Select(row => row.Select(v => JsonSerializer.SerializeToElement(v)).ToArray()).ToArray()
        };

        return new QueryApiResultCursor(
            response,
            query ?? AnyQuery,
            serverInfo ?? AnyServer,
            database ?? AnyDatabase);
    }

    [Fact]
    public async Task FetchAsync_ReturnsTrueWhileRowsRemain()
    {
        var cursor = MakeCursor(["name"], [["Alice"], ["Bob"]]);

        (await cursor.FetchAsync()).Should().BeTrue();
        (await cursor.FetchAsync()).Should().BeTrue();
        (await cursor.FetchAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Current_ReflectsLatestFetch()
    {
        var cursor = MakeCursor(["name"], [["Alice"], ["Bob"]]);

        await cursor.FetchAsync();
        cursor.Current["name"].Should().Be("Alice");

        await cursor.FetchAsync();
        cursor.Current["name"].Should().Be("Bob");
    }

    [Fact]
    public void Current_ThrowsBeforeFetch()
    {
        var cursor = MakeCursor(["name"], [["Alice"]]);
        cursor.Invoking(c => _ = c.Current).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task FetchAsync_WorksOnEmptyResponse()
    {
        var cursor = MakeCursor([], []);
        (await cursor.FetchAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task PeekAsync_ReturnsNextRecordWithoutAdvancing()
    {
        var cursor = MakeCursor(["n"], [[1L], [2L]]);

        var peeked = await cursor.PeekAsync();
        peeked!["n"].Should().Be(1L);

        // Position should not have advanced — fetch still returns the peeked row
        await cursor.FetchAsync();
        cursor.Current["n"].Should().Be(1L);
    }

    [Fact]
    public async Task PeekAsync_ReturnsNullAtEnd()
    {
        var cursor = MakeCursor(["n"], [[1L]]);
        await cursor.FetchAsync();

        var peeked = await cursor.PeekAsync();
        peeked.Should().BeNull();
    }

    [Fact]
    public async Task PeekAsync_ReturnsSameValueOnConsecutiveCalls()
    {
        var cursor = MakeCursor(["n"], [[42L]]);

        var first = await cursor.PeekAsync();
        var second = await cursor.PeekAsync();

        first!["n"].Should().Be(42L);
        second!["n"].Should().Be(42L);
    }

    [Fact]
    public async Task PeekThenFetch_AdvancesCorrectly()
    {
        var cursor = MakeCursor(["n"], [[1L], [2L], [3L]]);

        await cursor.PeekAsync();
        await cursor.FetchAsync();
        cursor.Current["n"].Should().Be(1L);

        await cursor.FetchAsync();
        cursor.Current["n"].Should().Be(2L);

        await cursor.FetchAsync();
        cursor.Current["n"].Should().Be(3L);

        (await cursor.FetchAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task KeysAsync_ReturnsFields()
    {
        var cursor = MakeCursor(["name", "age"], []);
        (await cursor.KeysAsync()).Should().Equal("name", "age");
    }

    [Fact]
    public void IsOpen_TrueInitially()
    {
        var cursor = MakeCursor([], []);
        cursor.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task IsOpen_FalseAfterConsume()
    {
        var cursor = MakeCursor([], []);
        await cursor.ConsumeAsync();
        cursor.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task FetchAsync_ThrowsAfterConsume()
    {
        var cursor = MakeCursor(["n"], [[1L]]);
        await cursor.ConsumeAsync();

        await cursor.Invoking(c => c.FetchAsync()).Should().ThrowAsync<ResultConsumedException>();
    }

    [Fact]
    public async Task PeekAsync_ThrowsAfterConsume()
    {
        var cursor = MakeCursor(["n"], [[1L]]);
        await cursor.ConsumeAsync();

        await cursor.Invoking(c => c.PeekAsync()).Should().ThrowAsync<ResultConsumedException>();
    }

    [Fact]
    public async Task Current_ThrowsAfterConsume()
    {
        var cursor = MakeCursor(["n"], [[1L]]);
        await cursor.ConsumeAsync();

        cursor.Invoking(c => _ = c.Current).Should().Throw<ResultConsumedException>();
    }

    [Fact]
    public async Task ConsumeAsync_DiscardsPendingRows()
    {
        var cursor = MakeCursor(["n"], [[1L], [2L]]);
        await cursor.FetchAsync();

        var summary = await cursor.ConsumeAsync();
        summary.Should().NotBeNull();
        cursor.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeAsync_SummaryContainsQuery()
    {
        var query = new Query("RETURN 42");
        var cursor = MakeCursor([], [], query: query);

        var summary = await cursor.ConsumeAsync();
        summary.Query.Should().Be(query);
    }

    [Fact]
    public async Task ConsumeAsync_SummaryContainsDatabase()
    {
        var cursor = MakeCursor([], [], database: "mydb");
        var summary = await cursor.ConsumeAsync();
        summary.Database.Name.Should().Be("mydb");
    }

    [Fact]
    public async Task ConsumeAsync_SummaryContainsServerInfo()
    {
        var serverInfo = new Mock<IServerInfo>().Object;
        var cursor = MakeCursor([], [], serverInfo: serverInfo);
        var summary = await cursor.ConsumeAsync();
        summary.Server.Should().BeSameAs(serverInfo);
    }

    [Fact]
    public async Task AsyncEnumerable_YieldsAllRows()
    {
        var cursor = MakeCursor(["n"], [[1L], [2L], [3L]]);

        var values = new List<long>();
        await foreach (var record in cursor)
            values.Add((long)record["n"]);

        values.Should().Equal(1L, 2L, 3L);
    }

    [Fact]
    public async Task Converts_StringValue()
    {
        var cursor = MakeCursor(["v"], [["hello"]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().Be("hello");
    }

    [Fact]
    public async Task Converts_LongValue()
    {
        var cursor = MakeCursor(["v"], [[long.MaxValue]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task Converts_DoubleValue()
    {
        var cursor = MakeCursor(["v"], [[3.14]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().Be(3.14);
    }

    [Fact]
    public async Task Converts_BoolTrue()
    {
        var cursor = MakeCursor(["v"], [[true]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().Be(true);
    }

    [Fact]
    public async Task Converts_BoolFalse()
    {
        var cursor = MakeCursor(["v"], [[false]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().Be(false);
    }

    [Fact]
    public async Task Converts_NullValue()
    {
        var cursor = MakeCursor(["v"], [[null]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().BeNull();
    }

    [Fact]
    public async Task Converts_ArrayValue()
    {
        var cursor = MakeCursor(["v"], [[(object)new[] { 1, 2, 3 }]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().BeEquivalentTo(new List<object?> { 1L, 2L, 3L });
    }

    [Fact]
    public async Task Converts_PlainObject_ToDictionary()
    {
        var cursor = MakeCursor(["v"], [[(object)new { x = 1, y = 2 }]]);
        await cursor.FetchAsync();
        var dict = cursor.Current["v"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        dict["x"].Should().Be(1L);
        dict["y"].Should().Be(2L);
    }

    [Fact]
    public async Task UnsupportedType_ReturnsStringPlaceholder()
    {
        // Simulate a $type-annotated value as the API would return for Neo4j types
        var typedValue = new Dictionary<string, object> { ["$type"] = "Node", ["_value"] = new { } };
        var cursor = MakeCursor(["v"], [[(object)typedValue]]);
        await cursor.FetchAsync();
        cursor.Current["v"].Should().Be("Unsupported type: Node");
    }

    [Fact]
    public async Task Record_SupportsMultipleFields()
    {
        var cursor = MakeCursor(["name", "age"], [["Alice", 30L]]);
        await cursor.FetchAsync();
        cursor.Current["name"].Should().Be("Alice");
        cursor.Current["age"].Should().Be(30L);
    }

    [Fact]
    public async Task Record_SupportsCaseInsensitiveLookup()
    {
        var cursor = MakeCursor(["Name"], [["Alice"]]);
        await cursor.FetchAsync();
        cursor.Current.GetCaseInsensitive<string>("name").Should().Be("Alice");
        cursor.Current.GetCaseInsensitive<string>("NAME").Should().Be("Alice");
    }
}
