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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class TransactionIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public TransactionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task ShouldRetry()
        {
            var session = Driver.AsyncSession();
            try
            {
                var timer = Stopwatch.StartNew();

                var exc = await Record.ExceptionAsync(() =>
                    session.WriteTransactionAsync(tx =>
                        throw new SessionExpiredException($"Failed at {timer.Elapsed}")));
                timer.Stop();

                exc.Should().BeOfType<ServiceUnavailableException>()
                    .Which.InnerException.Should().BeOfType<AggregateException>()
                    .Which.InnerExceptions.Should().NotBeEmpty().And.AllBeOfType<SessionExpiredException>();

                timer.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(30));
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldCommitTransactionByDefault()
        {
            var session = Driver.AsyncSession();
            try
            {
                var createResult =
                    await session.WriteTransactionAsync(tx =>
                        tx.RunAndSingleAsync("CREATE (n) RETURN count(n)", null));

                // the read operation should see the commited write tx
                var matchResult =
                    await session.ReadTransactionAsync(tx =>
                        tx.RunAndSingleAsync("MATCH (n) RETURN count(n)", null));

                createResult.Should().BeEquivalentTo(matchResult);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldNotCommitTransaction()
        {
            var session = Driver.AsyncSession();
            try
            {
                var createResult = await session.WriteTransactionAsync(async tx =>
                {
                    var result = await tx.RunAndSingleAsync("CREATE (n) RETURN count(n)", null, r => r[0].As<int>());
                    await tx.RollbackAsync();
                    return result;
                });

                // the read operation should not see the rolled back write tx
                var matchResult =
                    await session.ReadTransactionAsync(tx =>
                        tx.RunAndSingleAsync("MATCH (n) RETURN count(n)", null, r => r[0].As<int>()));

                createResult.Should().Be(matchResult + 1);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldNotCommitIfError()
        {
            var session = Driver.AsyncSession();
            try
            {
                var exc = await Record.ExceptionAsync(() => session.WriteTransactionAsync(async tx =>
                {
                    await tx.RunAsync("CREATE (n) RETURN count(n)");
                    throw new ProtocolException("Broken");
                }));
                exc.Should().NotBeNull();

                // the read operation should not see the rolled back write tx
                var matchResult =
                    await session.ReadTransactionAsync(tx =>
                        tx.RunAndSingleAsync("MATCH (n) RETURN count(n)", null, r => r[0].As<int>()));
                matchResult.Should().Be(0);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterRun()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
                    var txc = await session.BeginTransactionAsync();

                    var cursor = await txc.RunAsync("RETURN 1 As X");
                    var keys = await cursor.KeysAsync();

                    keys.Should().HaveCount(1);
                    keys.Should().Contain("X");

                    await txc.CommitAsync();
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterRunAndResultConsumption()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
                    var txc = await session.BeginTransactionAsync();
                    var cursor = await txc.RunAsync("RETURN 1 As X");

                    var keys = await cursor.KeysAsync();
                    keys.Should().BeEquivalentTo("X");

                    await cursor.ConsumeAsync();

                    keys = await cursor.KeysAsync();
                    keys.Should().BeEquivalentTo("X");

                    await txc.CommitAsync();
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRun()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
                    var txc = await session.BeginTransactionAsync();

                    var cursor1 = await txc.RunAsync("RETURN 1 As X");
                    var cursor2 = await txc.RunAsync("RETURN 1 As Y");

                    var keys1 = await cursor1.KeysAsync();
                    keys1.Should().BeEquivalentTo("X");

                    var keys2 = await cursor2.KeysAsync();
                    keys2.Should().BeEquivalentTo("Y");

                    await txc.CommitAsync();
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRunAndResultConsumption()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
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
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRunNoOrder()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
                    var txc = await session.BeginTransactionAsync();

                    var cursor1 = await txc.RunAsync("RETURN 1 As X");
                    var cursor2 = await txc.RunAsync("RETURN 1 As Y");

                    var keys2 = await cursor2.KeysAsync();
                    keys2.Should().BeEquivalentTo("Y");
                    var keys1 = await cursor1.KeysAsync();
                    keys1.Should().BeEquivalentTo("X");

                    await txc.CommitAsync();
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRunAndResultConsumptionNoOrder()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
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
                finally
                {
                    await session.CloseAsync();
                }
            }
        }
    }
}