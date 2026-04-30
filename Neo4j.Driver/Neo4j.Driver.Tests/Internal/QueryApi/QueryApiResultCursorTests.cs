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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using DriverRecord = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiResultCursorTests
{
    private static readonly Query AnyQuery = new("RETURN 1");

    private static readonly IResultSummaryFactory AnyFactory =
        new QueryApiResultSummaryFactory(new Mock<IServerInfo>().Object, "neo4j");

    // --- Helpers ---

    /// <summary>
    /// Builds a cursor from plain CLR values. Each inner array is one row; its elements map positionally to
    /// <paramref name="keys"/>.
    /// </summary>
    private static IResultCursor MakeCursor(
        string[] keys,
        object?[][] rows,
        Query? query = null,
        IResultSummaryFactory? summaryFactory = null)
    {
        var lookup = keys
            .Select((k, i) => (k, i))
            .ToDictionary(x => x.k, x => x.i, StringComparer.Ordinal);

        var invariantLookup = keys
            .Select((k, i) => (k, i))
            .ToDictionary(x => x.k, x => x.i, StringComparer.OrdinalIgnoreCase);

        var records = rows
            .Select(row => (IRecord)new DriverRecord(lookup, invariantLookup, row!))
            .ToList();

        return new QueryApiResultCursor(records, keys, query ?? AnyQuery, summaryFactory ?? AnyFactory);
    }

    // --- Iteration ---

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

    // --- Peek ---

    [Fact]
    public async Task PeekAsync_ReturnsNextRecordWithoutAdvancing()
    {
        var cursor = MakeCursor(["n"], [[1L], [2L]]);

        var peeked = await cursor.PeekAsync();
        peeked!["n"].Should().Be(1L);

        await cursor.FetchAsync();
        cursor.Current["n"].Should().Be(1L);
    }

    [Fact]
    public async Task PeekAsync_ReturnsNullAtEnd()
    {
        var cursor = MakeCursor(["n"], [[1L]]);
        await cursor.FetchAsync();

        (await cursor.PeekAsync()).Should().BeNull();
    }

    [Fact]
    public async Task PeekAsync_ReturnsSameValueOnConsecutiveCalls()
    {
        var cursor = MakeCursor(["n"], [[42L]]);

        (await cursor.PeekAsync())!["n"].Should().Be(42L);
        (await cursor.PeekAsync())!["n"].Should().Be(42L);
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

    // --- Keys ---

    [Fact]
    public async Task KeysAsync_ReturnsFields()
    {
        var cursor = MakeCursor(["name", "age"], []);
        (await cursor.KeysAsync()).Should().Equal("name", "age");
    }

    // --- IsOpen / ConsumeAsync ---

    [Fact]
    public void IsOpen_TrueInitially()
    {
        MakeCursor([], []).IsOpen.Should().BeTrue();
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

    // --- Summary ---

    [Fact]
    public async Task ConsumeAsync_SummaryContainsQuery()
    {
        var query = new Query("RETURN 42");
        var cursor = MakeCursor([], [], query);
        var summary = await cursor.ConsumeAsync();
        summary.Query.Should().Be(query);
    }

    [Fact]
    public async Task ConsumeAsync_SummaryContainsDatabase()
    {
        var factory = new QueryApiResultSummaryFactory(new Mock<IServerInfo>().Object, "mydb");
        var cursor = MakeCursor([], [], summaryFactory: factory);
        var summary = await cursor.ConsumeAsync();
        summary.Database.Name.Should().Be("mydb");
    }

    [Fact]
    public async Task ConsumeAsync_SummaryContainsServerInfo()
    {
        var serverInfo = new Mock<IServerInfo>().Object;
        var factory = new QueryApiResultSummaryFactory(serverInfo, "neo4j");
        var cursor = MakeCursor([], [], summaryFactory: factory);
        var summary = await cursor.ConsumeAsync();
        summary.Server.Should().BeSameAs(serverInfo);
    }

    // --- Async enumerable ---

    [Fact]
    public async Task AsyncEnumerable_YieldsAllRows()
    {
        var cursor = MakeCursor(["n"], [[1L], [2L], [3L]]);

        var values = new List<long>();
        await foreach (var record in cursor)
        {
            values.Add((long)record["n"]);
        }

        values.Should().Equal(1L, 2L, 3L);
    }

    // --- Multi-field / lookup ---

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
