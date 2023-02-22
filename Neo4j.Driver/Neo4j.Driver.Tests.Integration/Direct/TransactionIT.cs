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

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Extensions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class TransactionIT : DirectDriverTestBase
{
    public TransactionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    private IDriver Driver => Server.Driver;

    [RequireServerFact]
    public async Task ShouldRetry()
    {
        await using var session = Driver.AsyncSession();
        var timer = Stopwatch.StartNew();

        var exc = await Record.ExceptionAsync(
            () =>
                session.ExecuteWriteAsync(
                    _ =>
                        throw new SessionExpiredException($"Failed at {timer.Elapsed}")));

        timer.Stop();

        exc.Should()
            .BeOfType<ServiceUnavailableException>()
            .Which.InnerException.Should()
            .BeOfType<AggregateException>()
            .Which.InnerExceptions.Should()
            .NotBeEmpty()
            .And.AllBeOfType<SessionExpiredException>();

        timer.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(30));
    }

    [RequireServerFact]
    public async Task ShouldCommitTransactionByDefault()
    {
        await using var session = Driver.AsyncSession();
        var createResult =
            await session.ExecuteWriteAsync(
                tx =>
                    tx.RunAndSingleAsync("CREATE (n) RETURN count(n)", null));

        // the read operation should see the commited write tx
        var matchResult =
            await session.ExecuteReadAsync(
                tx =>
                    tx.RunAndSingleAsync("MATCH (n) RETURN count(n)", null));

        createResult.Should().BeEquivalentTo(matchResult);
    }

    [RequireServerFact]
    public async Task ShouldNotCommitTransaction()
    {
        var createResult = -1;
        await using var session = Driver.AsyncSession();
        try
        {
            await session.ExecuteWriteAsync(
                async tx =>
                {
                    createResult = await tx.RunAndSingleAsync(
                        "CREATE (n) RETURN count(n)",
                        null,
                        r => r[0].As<int>());

                    throw new InvalidOperationException("Throw in a transaction");
                });
        }
        catch (InvalidOperationException)
        {
            // ignore this exception!
            // it was thrown to cancel the transaction.
        }

        // the read operation should not see the rolled back write tx
        var matchResult =
            await session.ExecuteReadAsync(
                tx =>
                    tx.RunAndSingleAsync("MATCH (n) RETURN count(n)", null, r => r[0].As<int>()));

        createResult.Should().Be(matchResult + 1);
    }

    [RequireServerFact]
    public async Task ShouldNotCommitIfError()
    {
        await using var session = Driver.AsyncSession();
        var exc = await Record.ExceptionAsync(
            () => session.ExecuteWriteAsync(
                async tx =>
                {
                    await tx.RunAsync("CREATE (n) RETURN count(n)");
                    throw new ProtocolException("Broken");
                }));

        exc.Should().NotBeNull();

        // the read operation should not see the rolled back write tx
        var matchResult =
            await session.ExecuteReadAsync(
                tx =>
                    tx.RunAndSingleAsync("MATCH (n) RETURN count(n)", null, r => r[0].As<int>()));

        matchResult.Should().Be(0);
    }

    [RequireServerFact]
    public async Task KeysShouldBeAvailableAfterRun()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();

        var cursor = await txc.RunAsync("RETURN 1 As X");
        var keys = await cursor.KeysAsync();

        keys.Should().HaveCount(1);
        keys.Should().Contain("X");

        await txc.CommitAsync();
    }

    [RequireServerFact]
    public async Task KeysShouldBeAvailableAfterRunAndResultConsumption()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();
        var cursor = await txc.RunAsync("RETURN 1 As X");

        var keys = await cursor.KeysAsync();
        keys.Should().BeEquivalentTo("X");

        await cursor.ConsumeAsync();

        keys = await cursor.KeysAsync();
        keys.Should().BeEquivalentTo("X");

        await txc.CommitAsync();
    }

    [RequireServerFact]
    public async Task KeysShouldBeAvailableAfterConsecutiveRun()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();

        var cursor1 = await txc.RunAsync("RETURN 1 As X");
        var cursor2 = await txc.RunAsync("RETURN 1 As Y");

        var keys1 = await cursor1.KeysAsync();
        keys1.Should().BeEquivalentTo("X");

        var keys2 = await cursor2.KeysAsync();
        keys2.Should().BeEquivalentTo("Y");

        await txc.CommitAsync();
    }

    [RequireServerFact]
    public async Task KeysShouldBeAvailableAfterConsecutiveRunAndResultConsumption()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();

        var cursor1 = await txc.RunAsync("RETURN 1 As X");
        var cursor2 = await txc.RunAsync("RETURN 1 As Y");

        var keys1 = await cursor1.KeysAsync();
        keys1.Should().BeEquivalentTo("X");
        var keys2 = await cursor2.KeysAsync();
        keys2.Should().BeEquivalentTo("Y");

        await cursor1.ConsumeAsync();
        await cursor2.ConsumeAsync();

        keys1 = await cursor1.KeysAsync();
        keys1.Should().BeEquivalentTo("X");
        keys2 = await cursor2.KeysAsync();
        keys2.Should().BeEquivalentTo("Y");

        await txc.CommitAsync();
    }

    [RequireServerFact]
    public async Task KeysShouldBeAvailableAfterConsecutiveRunNoOrder()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();

        var cursor1 = await txc.RunAsync("RETURN 1 As X");
        var cursor2 = await txc.RunAsync("RETURN 1 As Y");

        var keys2 = await cursor2.KeysAsync();
        keys2.Should().BeEquivalentTo("Y");
        var keys1 = await cursor1.KeysAsync();
        keys1.Should().BeEquivalentTo("X");

        await txc.CommitAsync();
    }

    [RequireServerFact]
    public async Task KeysShouldBeAvailableAfterConsecutiveRunAndResultConsumptionNoOrder()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();

        var cursor1 = await txc.RunAsync("RETURN 1 As X");
        var cursor2 = await txc.RunAsync("RETURN 1 As Y");

        var keys2 = await cursor2.KeysAsync();
        keys2.Should().BeEquivalentTo("Y");
        var keys1 = await cursor1.KeysAsync();
        keys1.Should().BeEquivalentTo("X");

        await cursor2.ConsumeAsync();
        await cursor1.ConsumeAsync();

        keys2 = await cursor2.KeysAsync();
        keys2.Should().BeEquivalentTo("Y");
        keys1 = await cursor1.KeysAsync();
        keys1.Should().BeEquivalentTo("X");

        await txc.CommitAsync();
    }

    [RequireServerFact]
    public async Task ShouldNotBeAbleToAccessRecordsAfterRollback()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();
        var cursor = await txc.RunAsync("RETURN 1 As X");
        await txc.RollbackAsync();
        var error = await Record.ExceptionAsync(async () => await cursor.ToListAsync());
        error.Should().BeOfType<ResultConsumedException>();
    }

    [RequireServerFact]
    public async Task ShouldNotBeAbleToAccessRecordsAfterCommit()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();
        var cursor = await txc.RunAsync("RETURN 1 As X");
        await txc.CommitAsync();
        var error = await Record.ExceptionAsync(async () => await cursor.ToListAsync());
        error.Should().BeOfType<ResultConsumedException>();
    }

    [RequireServerFact]
    public async Task ShouldNotBeAbleToAccessRecordsAfterSummary()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();
        var cursor = await txc.RunAsync("RETURN 1 As X");
        await cursor.ConsumeAsync();

        var error = await Record.ExceptionAsync(async () => await cursor.ToListAsync());
        error.Should().BeOfType<ResultConsumedException>();
        await txc.RollbackAsync();
    }

    [RequireServerFact]
    public async Task ShouldBeAbleToRunNestedQueries()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, o => o.WithFetchSize(2));
        const int size = 1024;
        await using var session = driver.AsyncSession(o => o.WithFetchSize(5));
        var txc1 = await session.BeginTransactionAsync();
        var cursor1 = await txc1.RunAsync("UNWIND range(1, $size) AS x RETURN x", new { size });

        await cursor1.ForEachAsync(
            r =>
                txc1.RunAsync(
                    "UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id",
                    new { x = r["x"].As<int>() }));

        var count = await (await txc1.RunAsync("MATCH (n:Node) RETURN count(n)")).SingleAsync();
        count[0].As<int>().Should().Be(size);
        await txc1.RollbackAsync();
    }
}
