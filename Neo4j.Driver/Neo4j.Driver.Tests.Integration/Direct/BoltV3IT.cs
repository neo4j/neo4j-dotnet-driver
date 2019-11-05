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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class BoltV3IT : DirectDriverTestBase
    {
        public BoltV3IT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact("3.5.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldRunWithTxConfigAsync()
        {
            // When
            var session = Server.Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync("CALL dbms.listTransactions()",
                    o => { o.WithMetadata(new Dictionary<string, object> {{"name", "Molly"}}); });
                var records = await cursor.ToListAsync(r => r["metaData"].As<IDictionary<string, object>>());

                // Then
                records.Single().Should().HaveCount(1).And.Contain("name", "Molly");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact("3.5.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldRunWithTxTimeoutAsync()
        {
            // Given
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE (:Node)").Summary();
            }

            var otherSession = Server.Driver.AsyncSession();
            try
            {
                var otherTx = await otherSession.BeginTransactionAsync();
                try
                {
                    // lock dummy node but keep the transaction open
                    await otherTx.RunAsync("MATCH (n:Node) SET n.prop = 1").ContinueWith(t => t.Result.SummaryAsync())
                        .Unwrap();

                    // When
                    // run a query in an auto-commit transaction with timeout and try to update the locked dummy node
                    var session = Server.Driver.AsyncSession();
                    try
                    {
                        var error = await Record.ExceptionAsync(() =>
                            session.RunAsync("MATCH (n:Node) SET n.prop = 2",
                                    o => { o.WithTimeout(TimeSpan.FromMilliseconds(1)); })
                                .ContinueWith(c => c.Result.SummaryAsync()).Unwrap());

                        // Then
                        error.Should().BeOfType<TransientException>().Which.Message.Should().Contain("terminated");
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
                finally
                {
                    await otherTx.CommitAsync();
                }
            }
            finally
            {
                await otherSession.CloseAsync();
            }
        }

        [RequireServerFact("3.5.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldReadWithTxConfigAsync()
        {
            await RunWithTxConfigAsync(true);
        }

        [RequireServerFact("3.5.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldWriteWithTxConfigAsync()
        {
            await RunWithTxConfigAsync(false);
        }

        private async Task RunWithTxConfigAsync(bool read)
        {
            // Given
            void BuildOptions(TransactionOptions o)
            {
                o.WithMetadata(new Dictionary<string, object> {{"name", "Molly"}});
            }

            // When
            var session = Server.Driver.AsyncSession();
            try
            {
                var single = read
                    ? await session.ReadTransactionAsync(tx =>
                        tx.RunAsync("CALL dbms.listTransactions()")
                            .ContinueWith(cursor => cursor.Result.SingleAsync())
                            .Unwrap(), BuildOptions)
                    : await session.WriteTransactionAsync(tx =>
                        tx.RunAsync("CALL dbms.listTransactions()")
                            .ContinueWith(cursor => cursor.Result.SingleAsync())
                            .Unwrap(), BuildOptions);

                // Then
                var value = single["metaData"].As<IDictionary<string, object>>();
                value.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("name", "Molly"));
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}