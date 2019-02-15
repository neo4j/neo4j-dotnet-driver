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
using FluentAssertions;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.IntegrationTests
{
    public class ResultIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public ResultIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {}

        [RequireServerFact]
        public void GetSummary()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE (p:Person { Name: 'Test'})");
                var summary = result.Consume();
                result.Peek().Should().BeNull();

                summary.Statement.Text.Should().Be("CREATE (p:Person { Name: 'Test'})");
                summary.Statement.Parameters.Count.Should().Be(0);

                var stats = summary.Counters;
                stats.ToString().Should()
                    .Be("Counters{NodesCreated=1, NodesDeleted=0, RelationshipsCreated=0, " +
                    "RelationshipsDeleted=0, PropertiesSet=1, LabelsAdded=1, LabelsRemoved=0, " +
                    "IndexesAdded=0, IndexesRemoved=0, ConstraintsAdded=0, ConstraintsRemoved=0}");

                summary.StatementType.Should().Be(StatementType.WriteOnly);

                var serverInfo = result.Summary.Server;

                serverInfo.Address.Should().Be("localhost:7687");
                if (ServerVersion.Version(serverInfo.Version) >= ServerVersion.V3_1_0)
                {
                    result.Summary.ResultAvailableAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                    result.Summary.ResultConsumedAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                }
                else
                {
                    result.Summary.ResultAvailableAfter.Should().BeLessThan(TimeSpan.Zero);
                    result.Summary.ResultConsumedAfter.Should().BeLessThan(TimeSpan.Zero);
                }
            }
        }

        [RequireServerFact]
        public void GetPlan()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("EXPLAIN MATCH (n) RETURN 1");
                var summary = result.Consume();

                summary.HasPlan.Should().BeTrue();
                summary.HasProfile.Should().BeFalse();

                var plan = summary.Plan;
                plan.Identifiers.Count.Should().BePositive();
                plan.Arguments.Count.Should().BePositive();
                plan.Children.Count.Should().BePositive();
                plan.OperatorType.Should().NotBeNullOrEmpty();
            }
        }

        [RequireServerFact]
        public void GetProfile()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("PROFILE RETURN 1");
                var summary = result.Consume();

                summary.HasPlan.Should().BeTrue();
                summary.HasProfile.Should().BeTrue();

                summary.Plan.Should().Be(summary.Profile);

                var profile = summary.Profile;
                profile.DbHits.Should().Be(0L);
                profile.Records.Should().Be(1L);
            }
        }

        [RequireServerFact]
        public void GetNotification()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("EXPLAIN MATCH (n), (m) RETURN n, m");
                var summary = result.Consume();

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
        }

        [RequireServerFact]
        public void AccessSummaryAfterFailure()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("Invalid");
                var error = Record.Exception(() => result.Consume());
                error.Should().BeOfType<ClientException>();
                var summary = result.Summary;

                summary.Should().NotBeNull();
                summary.Counters.NodesCreated.Should().Be(0);
                summary.Server.Address.Should().Contain("localhost:7687");
            }
        }

        [RequireServerFact]
        public void BufferRecordsAfterSummary()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("UNWIND [1,2] AS a RETURN a");
                var summary = result.Summary;

                summary.Should().NotBeNull();
                summary.Counters.NodesCreated.Should().Be(0);
                summary.Server.Address.Should().Contain("localhost:7687");

                result.First()["a"].ValueAs<int>().Should().Be(1);
                result.First()["a"].ValueAs<int>().Should().Be(2);
            }
        }

        [RequireServerFact]
        public void DiscardRecordsAfterConsume()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("UNWIND [1,2] AS a RETURN a");
                var summary = result.Consume();

                summary.Should().NotBeNull();
                summary.Counters.NodesCreated.Should().Be(0);
                summary.Server.Address.Should().Contain("localhost:7687");

                result.ToList().Count.Should().Be(0);
            }
        }

        [RequireServerFact]
        public void BuffersResultsOfRunSoTheyCanBeReadAfterAnotherSubsequentRun()
        {
            using (var session = Driver.Session())
            {
                var result1 = session.Run("unwind range(1,3) as n RETURN n");
                var result2 = session.Run("unwind range(4,6) as n RETURN n");

                var result2All = result2.ToList();
                var result1All = result1.ToList();

                result2All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(4, 5, 6);
                result1All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1, 2, 3);
            }
        }

        [RequireServerFact]
        public void BufferResultAfterSessionClose()
        {
            IStatementResult result;
            using (var session = Driver.Session())
            {
                result = session.Run("unwind range(1,3) as n RETURN n");
            }
            var resultAll = result.ToList();

            // Records that has not been read inside session still saved
            resultAll.Count.Should().Be(3);
            resultAll.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1, 2, 3);

            // Summary is still saved
            result.Summary.Statement.Text.Should().Be("unwind range(1,3) as n RETURN n");
            result.Summary.StatementType.Should().Be(StatementType.ReadOnly);
        }

        [RequireServerFact]
        public void BuffersResultsAfterTxCloseSoTheyCanBeReadAfterAnotherSubsequentTx()
        {
            using (var session = Driver.Session())
            {
                IStatementResult result1, result2;
                using (var tx = session.BeginTransaction())
                {
                    result1 = tx.Run("unwind range(1,3) as n RETURN n");
                    tx.Success();
                }

                using (var tx = session.BeginTransaction())
                {
                    result2 = tx.Run("unwind range(4,6) as n RETURN n");
                    tx.Success();
                }

                var result2All = result2.ToList();
                var result1All = result1.ToList();

                result2All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(4, 5, 6);
                result1All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1, 2, 3);
            }
        }
    }
}
