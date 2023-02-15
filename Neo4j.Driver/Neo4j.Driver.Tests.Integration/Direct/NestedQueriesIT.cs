// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class NestedQueriesIT : DirectDriverTestBase
{
    public NestedQueriesIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithSessionRuns()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var cursor1 = await session.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
        var error = await Record.ExceptionAsync(
            async () =>
            {
                while (await cursor1.FetchAsync())
                {
                    var record = cursor1.Current;
                    await session.RunAsync(
                        "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                        new { x = record["x"].As<int>() });
                }
            });

        error.Should()
            .BeOfType<ResultConsumedException>()
            .Which.Message.Should()
            .Contain("result has already been consumed");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithSessionAndTxRuns()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var cursor1 = await session.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
        var error = await Record.ExceptionAsync(
            async () =>
            {
                while (await cursor1.FetchAsync())
                {
                    await session.BeginTransactionAsync();
                }
            });

        error.Should()
            .BeOfType<ResultConsumedException>()
            .Which.Message.Should()
            .Contain("result has already been consumed");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithSessionRunAndTxFunc()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));

        var cursor1 = await session.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });

        var error = await Record.ExceptionAsync(
            async () =>
            {
                while (await cursor1.FetchAsync())
                {
                    var record = cursor1.Current;
                    await session.ExecuteWriteAsync(
                        async tx => await tx.RunAsync(
                            "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                            new { x = record["x"].As<int>() }));
                }
            });

        error.Should()
            .BeOfType<ResultConsumedException>()
            .Which.Message.Should()
            .Contain("result has already been consumed");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithTxAndSessionRuns()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var tx = await session.BeginTransactionAsync();
        var cursor1 = await tx.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
        var error = await Record.ExceptionAsync(
            async () =>
            {
                while (await cursor1.FetchAsync())
                {
                    var record = cursor1.Current;
                    await session.RunAsync(
                        "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                        new { x = record["x"].As<int>() });
                }
            });

        error.Should()
            .BeOfType<TransactionNestingException>()
            .Which.Message.Should()
            .Contain("Attempting to nest transactions");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithTxRuns()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var tx = await session.BeginTransactionAsync();
        var cursor1 = await tx.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
        var error = await Record.ExceptionAsync(
            async () =>
            {
                while (await cursor1.FetchAsync())
                {
                    await session.BeginTransactionAsync();
                }
            });

        error.Should()
            .BeOfType<TransactionNestingException>()
            .Which.Message.Should()
            .Contain("Attempting to nest transactions");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithTxRunAndTxFunc()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var tx = await session.BeginTransactionAsync();
        var cursor1 = await tx.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
        var error = await Record.ExceptionAsync(
            async () =>
            {
                while (await cursor1.FetchAsync())
                {
                    var record = cursor1.Current;
                    await session.ExecuteWriteAsync(
                        async tx2 => await tx2.RunAsync(
                            "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                            new { x = record["x"].As<int>() }));
                }
            });

        error.Should()
            .BeOfType<TransactionNestingException>()
            .Which.Message.Should()
            .Contain("Attempting to nest transactions");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithTxFuncs()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var error = await Record.ExceptionAsync(
            async () =>
                await session.ExecuteReadAsync(
                    async tx =>
                    {
                        var cursor1 = await tx.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
                        while (await cursor1.FetchAsync())
                        {
                            var record = cursor1.Current;
                            await session.ExecuteWriteAsync(
                                async tx2 => await tx2.RunAsync(
                                    "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                                    new { x = record["x"].As<int>() }));
                        }
                    }));

        error.Should()
            .BeOfType<TransactionNestingException>()
            .Which.Message.Should()
            .Contain("Attempting to nest transactions");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithTxFuncAndSessionRun()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var error = await Record.ExceptionAsync(
            async () =>
                await session.ExecuteReadAsync(
                    async tx =>
                    {
                        var cursor1 = await tx.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
                        while (await cursor1.FetchAsync())
                        {
                            var record = cursor1.Current;
                            await session.RunAsync(
                                "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                                new { x = record["x"].As<int>() });
                        }
                    }));

        error.Should()
            .BeOfType<TransactionNestingException>()
            .Which.Message.Should()
            .Contain("Attempting to nest transactions");
    }

    [RequireServerFact]
    public async Task ShouldErrorToRunNestedQueriesWithTxFuncAndTxRun()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var error = await Record.ExceptionAsync(
            async () =>
                await session.ExecuteReadAsync(
                    async tx =>
                    {
                        var cursor1 = await tx.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });
                        while (await cursor1.FetchAsync())
                        {
                            await session.BeginTransactionAsync();
                        }
                    }));

        error.Should()
            .BeOfType<TransactionNestingException>()
            .Which.Message.Should()
            .Contain("Attempting to nest transactions");
    }
}
