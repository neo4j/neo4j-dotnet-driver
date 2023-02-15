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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Tests;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;
using static Neo4j.Driver.SessionConfigBuilder;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class ResultIT : DirectDriverTestBase
{
    public ResultIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    private IDriver Driver => Server.Driver;

    [RequireServerFact]
    public async Task GetSummary()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync("CREATE (p:Person { Name: 'Test'})");
            var summary = await cursor.ConsumeAsync();

            summary.Query.Text.Should().Be("CREATE (p:Person { Name: 'Test'})");
            summary.Query.Parameters.Count.Should().Be(0);

            var stats = summary.Counters;
            stats.ToString()
                .Should()
                .Be(
                    "Counters{NodesCreated=1, NodesDeleted=0, RelationshipsCreated=0, " +
                    "RelationshipsDeleted=0, PropertiesSet=1, LabelsAdded=1, LabelsRemoved=0, " +
                    "IndexesAdded=0, IndexesRemoved=0, ConstraintsAdded=0, ConstraintsRemoved=0, SystemUpdates=0}");

            summary.QueryType.Should().Be(QueryType.WriteOnly);

            var serverInfo = summary.Server;

            var boltAddress = DefaultInstallation.BoltUri.Replace("bolt://", string.Empty);
            serverInfo.Address.Should().Be(boltAddress);
            summary.ResultAvailableAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
            summary.ResultConsumedAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldContainsSystemUpdates()
    {
        // Ensure that a constraint exists
        var session = Driver.AsyncSession(ForDatabase("system"));
        try
        {
            var cursor = await session.RunAsync("CREATE USER foo SET PASSWORD 'bar'");
            var summary = await cursor.ConsumeAsync();
            summary.Counters.ContainsUpdates.Should().BeFalse();
            summary.Counters.ContainsSystemUpdates.Should().BeTrue();
            summary.Counters.SystemUpdates.Should().Be(1);
        }
        finally
        {
            var cursor = await session.RunAsync("DROP USER foo");
            await cursor.ConsumeAsync();
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task GetPlan()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync("EXPLAIN MATCH (n) RETURN 1");
            var summary = await cursor.ConsumeAsync();

            summary.HasPlan.Should().BeTrue();
            summary.HasProfile.Should().BeFalse();

            var plan = summary.Plan;
            plan.Identifiers.Count.Should().BePositive();
            plan.Arguments.Count.Should().BePositive();
            plan.Children.Count.Should().BePositive();
            plan.OperatorType.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task GetProfile()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync("PROFILE RETURN 1");
            var summary = await cursor.ConsumeAsync();

            summary.HasPlan.Should().BeTrue();
            summary.HasProfile.Should().BeTrue();

            summary.Plan.Should().Be(summary.Profile);

            var profile = summary.Profile;
            profile.DbHits.Should().Be(0L);
            profile.Records.Should().Be(1L);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task GetNotification()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync("EXPLAIN MATCH (n:ThisLabelDoesNotExist) RETURN n");
            var summary = await cursor.ConsumeAsync();

            var notifications = summary.Notifications;
            notifications.Should().NotBeNull();
            notifications.Count.Should().Be(1);
            var notification = notifications[0];

            notification.Code.Should().NotBeNullOrEmpty();
            notification.Description.Should().NotBeNullOrEmpty();
            notification.Title.Should().NotBeNullOrEmpty();
            notification.Severity.Should().NotBeNullOrEmpty();
            notification.Position.Should().NotBeNull();
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task AccessSummaryAfterFailure()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync("Invalid");
            var error = await Record.ExceptionAsync(() => cursor.ConsumeAsync());
            error.Should().BeOfType<ClientException>();

            var summary = await cursor.ConsumeAsync();

            var boltAddress = DefaultInstallation.BoltUri.Replace(
                DefaultInstallation.BoltHeader,
                string.Empty);

            summary.Should().NotBeNull();
            summary.Counters.NodesCreated.Should().Be(0);
            summary.Server.Address.Should().Contain(boltAddress);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task ErrorAccessRecordsAfterConsume()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync("unwind range(1,3) as n return n");
            var summary = await cursor.ConsumeAsync();

            var boltAddress = DefaultInstallation.BoltUri.Replace(
                DefaultInstallation.BoltHeader,
                string.Empty);

            summary.Should().NotBeNull();
            summary.Counters.NodesCreated.Should().Be(0);
            summary.Server.Address.Should().Contain(boltAddress);

            await AssertCannotAccessRecords(cursor);
            await CanAccessSummary(cursor);
            await CanAccessKeys(cursor);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task BuffersResultsOfRunSoTheyCanBeReadAfterAnotherSubsequentRun()
    {
        var session = Driver.AsyncSession();
        try
        {
            var cursor1 = await session.RunAsync("unwind range(1,3) as n RETURN n");
            var cursor2 = await session.RunAsync("unwind range(4,6) as n RETURN n");

            await AssertCannotAccessRecords(cursor1);
            await CanAccessSummary(cursor1);
            await CanAccessKeys(cursor1);

            var list2 = await cursor2.ToListAsync(r => r["n"].As<int>());
            list2.Should().ContainInOrder(4, 5, 6);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact]
    public async Task ErrorAccessRecordsAfterSessionClose()
    {
        IResultCursor cursor;
        var session = Driver.AsyncSession();
        try
        {
            cursor = await session.RunAsync("unwind range(1,3) as n RETURN n");
        }
        finally

        {
            await session.CloseAsync();
        }

        await AssertCannotAccessRecords(cursor);
        await CanAccessSummary(cursor);
        await CanAccessKeys(cursor);
    }

    [RequireServerFact]
    public async Task ErrorAccessRecordsAfterTxClose()
    {
        var session = Driver.AsyncSession();
        try
        {
            IResultCursor result1, result2;

            var tx1 = await session.BeginTransactionAsync();
            try
            {
                result1 = await tx1.RunAsync("unwind range(1,3) as n RETURN n");
                await tx1.CommitAsync();
            }
            catch
            {
                await tx1.RollbackAsync();
                throw;
            }

            var tx2 = await session.BeginTransactionAsync();
            try
            {
                result2 = await tx2.RunAsync("unwind range(4,6) as n RETURN n");
                await tx2.CommitAsync();
            }
            catch
            {
                await tx2.RollbackAsync();
                throw;
            }

            await AssertCannotAccessRecords(result1);
            await CanAccessSummary(result1);
            await CanAccessKeys(result1);

            await AssertCannotAccessRecords(result2);
            await CanAccessSummary(result2);
            await CanAccessKeys(result2);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private static async Task CanAccessKeys(IResultCursor cursor)
    {
        // Summary is still saved
        var keys = await cursor.KeysAsync();
        keys.Should().ContainInOrder("n");
    }

    private static async Task CanAccessSummary(IResultCursor cursor)
    {
        // Summary is still saved
        var summary = await cursor.ConsumeAsync();
        summary.Query.Text.ToLower().Should().NotBeNullOrEmpty();
        summary.QueryType.Should().Be(QueryType.ReadOnly);
    }

    private static async Task AssertCannotAccessRecords(IResultCursor cursor)
    {
        await ConsumedException.ThrowsResultConsumedException(() => cursor.FetchAsync());
        await ConsumedException.ThrowsResultConsumedException(() => cursor.PeekAsync());
        ConsumedException.ThrowsResultConsumedException(() => cursor.Current);
        await ConsumedException.ThrowsResultConsumedException(() => cursor.SingleAsync());
        await ConsumedException.ThrowsResultConsumedException(() => cursor.ToListAsync());
        await ConsumedException.ThrowsResultConsumedException(() => cursor.ForEachAsync(_ => {}));
    }
}
