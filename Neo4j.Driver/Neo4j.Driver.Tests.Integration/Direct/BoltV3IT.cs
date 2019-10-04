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
            // Given
            var txConfig = new TransactionConfig {Metadata = new Dictionary<string, object> {{"name", "Molly"}}};

            // When
            var session = Server.Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync("CALL dbms.listTransactions()", txConfig);
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
                session.Run("CREATE (:Node)").Consume();
            }

            var otherSession = Server.Driver.AsyncSession();
            try
            {
                var otherTx = await otherSession.BeginTransactionAsync();
                try
                {
                    // lock dummy node but keep the transaction open
                    await otherTx.RunAsync("MATCH (n:Node) SET n.prop = 1").ContinueWith(t => t.Result.ConsumeAsync())
                        .Unwrap();

                    // When
                    // run a query in an auto-commit transaction with timeout and try to update the locked dummy node
                    var txConfig = new TransactionConfig {Timeout = TimeSpan.FromMilliseconds(1)};
                    var session = Server.Driver.AsyncSession();
                    try
                    {
                        var error = await Record.ExceptionAsync(() =>
                            session.RunAsync("MATCH (n:Node) SET n.prop = 2", txConfig)
                                .ContinueWith(c => c.Result.ConsumeAsync()).Unwrap());

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
            var txConfig = new TransactionConfig {Metadata = new Dictionary<string, object> {{"name", "Molly"}}};

            // When
            var session = Server.Driver.AsyncSession();
            try
            {
                var result = read
                    ? await session.ReadTransactionAsync(tx => tx.RunAsync("CALL dbms.listTransactions()"), txConfig)
                    : await session.WriteTransactionAsync(tx => tx.RunAsync("CALL dbms.listTransactions()"), txConfig);

                // Then
                var value = (await result.SingleAsync())["metaData"].As<IDictionary<string, object>>();
                value.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("name", "Molly"));
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}