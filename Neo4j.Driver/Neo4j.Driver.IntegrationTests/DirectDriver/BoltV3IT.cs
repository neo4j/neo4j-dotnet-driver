// Copyright (c) "Neo4j"
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
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class BoltV3IT : DirectDriverTestBase
    {
        public BoltV3IT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public void ShouldRunWithTxConfig()
        {
            // Given
            var txConfig = new TransactionConfig {Metadata = new Dictionary<string, object> {{"name", "Molly"}}};

            // When
            using (var session = Server.Driver.Session())
            {
                var result = session.Run("CALL dbms.listTransactions()", txConfig);
                // Then
                var value = result.Single()["metaData"].ValueAs<IDictionary<string, object>>();
                value.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("name", "Molly"));
                result.Summary.ToString().Should().Contain("ResultAvailableAfter");
                result.Summary.ToString().Should().Contain("ResultConsumedAfter");
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public async Task ShouldRunWithTxConfigAsync()
        {
            // Given
            var txConfig = new TransactionConfig {Metadata = new Dictionary<string, object> {{"name", "Molly"}}};

            // When
            using (var session = Server.Driver.Session())
            {
                var result = await session.RunAsync("CALL dbms.listTransactions()", txConfig);
                // Then
                var value = (await result.SingleAsync())["metaData"].ValueAs<IDictionary<string, object>>();
                value.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("name", "Molly"));
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public void ShouldRunWithTxTimeout()
        {
            // Given
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE (:Node)").Consume();
            }

            using (var otherSession = Server.Driver.Session())
            {
                using (var otherTx = otherSession.BeginTransaction())
                {
                    // lock dummy node but keep the transaction open
                    otherTx.Run("MATCH (n:Node) SET n.prop = 1").Consume();

                    // When
                    // run a query in an auto-commit transaction with timeout and try to update the locked dummy node
                    var txConfig = new TransactionConfig {Timeout = TimeSpan.FromMilliseconds(1)};
                    using (var session = Server.Driver.Session())
                    {
                        var error = Xunit.Record.Exception(() =>
                            session.Run("MATCH (n:Node) SET n.prop = 2", txConfig).Consume());
                        // Then
                        error.Should().BeOfType<TransientException>();
                        error.Message.Should().Contain("terminated");
                    }
                }
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public async Task ShouldRunWithTxTimeoutAsync()
        {
            // Given
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE (:Node)").Consume();
            }

            using (var otherSession = Server.Driver.Session())
            {
                using (var otherTx = otherSession.BeginTransaction())
                {
                    // lock dummy node but keep the transaction open
                    otherTx.Run("MATCH (n:Node) SET n.prop = 1").Consume();

                    // When
                    // run a query in an auto-commit transaction with timeout and try to update the locked dummy node
                    var txConfig = new TransactionConfig {Timeout = TimeSpan.FromMilliseconds(1)};
                    using (var session = Server.Driver.Session())
                    {
                        var error = await Xunit.Record.ExceptionAsync(() => session.RunAsync("MATCH (n:Node) SET n.prop = 2", txConfig));
                        // Then
                        error.Should().BeOfType<TransientException>();
                        error.Message.Should().Contain("terminated");
                    }
                }
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public void ShouldReadWithTxConfig()
        {
            RunWithTxConfig(true);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public async Task ShouldReadWithTxConfigAsync()
        {
            await RunWithTxConfigAsync(true);
        }
        
        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public void ShouldWriteWithTxConfig()
        {
            RunWithTxConfig(false);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.5.0")]
        public async Task ShouldWriteWithTxConfigAsync()
        {
            await RunWithTxConfigAsync(false);
        }
        
        private void RunWithTxConfig(bool read)
        {
            // Given
            var txConfig = new TransactionConfig {Metadata = new Dictionary<string, object> {{"name", "Molly"}}};

            // When
            using (var session = Server.Driver.Session())
            {
                var result = read
                    ? session.ReadTransaction(tx => tx.Run("CALL dbms.listTransactions()"), txConfig)
                    : session.WriteTransaction(tx => tx.Run("CALL dbms.listTransactions()"), txConfig);
                // Then
                var value = result.Single()["metaData"].ValueAs<IDictionary<string, object>>();
                value.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("name", "Molly"));
            }
        }
        
        private async Task RunWithTxConfigAsync(bool read)
        {
            // Given
            var txConfig = new TransactionConfig {Metadata = new Dictionary<string, object> {{"name", "Molly"}}};

            // When
            using (var session = Server.Driver.Session())
            {
                var result = read
                    ? await session.ReadTransactionAsync(tx => tx.RunAsync("CALL dbms.listTransactions()"), txConfig)
                    : await session.WriteTransactionAsync(tx => tx.RunAsync("CALL dbms.listTransactions()"), txConfig);

                // Then
                var value = (await result.SingleAsync())["metaData"].ValueAs<IDictionary<string, object>>();
                value.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("name", "Molly"));
            }
        }

    }
}