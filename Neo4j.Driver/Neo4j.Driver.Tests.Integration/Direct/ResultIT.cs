// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Util;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class ResultIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public ResultIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task GetSummary()
        {
            var session = Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync("CREATE (p:Person { Name: 'Test'})");
                var summary = await cursor.ConsumeAsync();

                var peeked = await cursor.PeekAsync();
                peeked.Should().BeNull();

                summary.Statement.Text.Should().Be("CREATE (p:Person { Name: 'Test'})");
                summary.Statement.Parameters.Count.Should().Be(0);

                var stats = summary.Counters;
                stats.ToString().Should()
                    .Be("Counters{NodesCreated=1, NodesDeleted=0, RelationshipsCreated=0, " +
                        "RelationshipsDeleted=0, PropertiesSet=1, LabelsAdded=1, LabelsRemoved=0, " +
                        "IndexesAdded=0, IndexesRemoved=0, ConstraintsAdded=0, ConstraintsRemoved=0}");

                summary.StatementType.Should().Be(StatementType.WriteOnly);

                var serverInfo = summary.Server;

                serverInfo.Address.Should().Be("localhost:7687");
                if (ServerVersion.From(serverInfo.Version) >= ServerVersion.V3_1_0)
                {
                    summary.ResultAvailableAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                    summary.ResultConsumedAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                }
                else
                {
                    summary.ResultAvailableAfter.Should().BeLessThan(TimeSpan.Zero);
                    summary.ResultConsumedAfter.Should().BeLessThan(TimeSpan.Zero);
                }
            }
            finally
            {
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
                var cursor = await session.RunAsync("EXPLAIN MATCH (n), (m) RETURN n, m");
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

                var summary = await cursor.SummaryAsync();

                summary.Should().NotBeNull();
                summary.Counters.NodesCreated.Should().Be(0);
                summary.Server.Address.Should().Contain("localhost:7687");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task BufferRecordsAfterSummary()
        {
            var session = Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync("UNWIND [1,2] AS a RETURN a");
                var summary = await cursor.SummaryAsync();

                summary.Should().NotBeNull();
                summary.Counters.NodesCreated.Should().Be(0);
                summary.Server.Address.Should().Contain("localhost:7687");

                var next = await cursor.FetchAsync();
                next.Should().BeTrue();
                cursor.Current["a"].Should().BeEquivalentTo(1);

                next = await cursor.FetchAsync();
                next.Should().BeTrue();
                cursor.Current["a"].Should().BeEquivalentTo(2);

                next = await cursor.FetchAsync();
                next.Should().BeFalse();
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task DiscardRecordsAfterConsume()
        {
            var session = Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync("UNWIND [1,2] AS a RETURN a");
                var summary = await cursor.ConsumeAsync();

                summary.Should().NotBeNull();
                summary.Counters.NodesCreated.Should().Be(0);
                summary.Server.Address.Should().Contain("localhost:7687");

                var list = await cursor.ToListAsync();
                list.Should().BeEmpty();
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

                var list2 = await cursor2.ToListAsync(r => r["n"].As<int>());
                var list1 = await cursor1.ToListAsync(r => r["n"].As<int>());

                list2.Should().ContainInOrder(4, 5, 6);
                list1.Should().ContainInOrder(1, 2, 3);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task BufferResultAfterSessionClose()
        {
            IStatementResultCursor cursor;
            var session = Driver.AsyncSession();
            try
            {
                cursor = await session.RunAsync("unwind range(1,3) as n RETURN n");
            }
            finally

            {
                await session.CloseAsync();
            }

            var resultAll = await cursor.ToListAsync(r => r["n"].As<int>());

            // Records that has not been read inside session still saved
            resultAll.Count.Should().Be(3);
            resultAll.Should().ContainInOrder(1, 2, 3);

            // Summary is still saved
            var summary = await cursor.SummaryAsync();
            summary.Statement.Text.Should().Be("unwind range(1,3) as n RETURN n");
            summary.StatementType.Should().Be(StatementType.ReadOnly);
        }

        [RequireServerFact]
        public async Task BuffersResultsAfterTxCloseSoTheyCanBeReadAfterAnotherSubsequentTx()
        {
            var session = Driver.AsyncSession();
            try
            {
                IStatementResultCursor result1, result2;

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

                var result2All = await result2.ToListAsync(r => r["n"].As<int>());
                var result1All = await result1.ToListAsync(r => r["n"].As<int>());

                result2All.Should().ContainInOrder(4, 5, 6);
                result1All.Should().ContainInOrder(1, 2, 3);
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}