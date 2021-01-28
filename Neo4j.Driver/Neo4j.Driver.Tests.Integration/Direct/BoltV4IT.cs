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
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.VersionComparison;
using static Neo4j.Driver.IntegrationTests.DatabaseExtensions;
using Neo4j.Driver.Internal.Util;
using Neo4j.Driver.IntegrationTests.Internals;



namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class BoltV4IT : DirectDriverTestBase
    {
        public BoltV4IT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDefaultDatabase()
        {
            await VerifyDatabaseNameOnSummary(null, "neo4j");
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDefaultDatabaseWhenSpecified()
        {
            await VerifyDatabaseNameOnSummary("neo4j", "neo4j");
        }

        [RequireEnterpriseEdition("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDatabase()
        {
            await CreateDatabase(Server.Driver, "foo");
            try
            {
                await VerifyDatabaseNameOnSummary("foo", "foo");
            }
            finally
            {
                await DropDatabase(Server.Driver, "foo");
            }
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDefaultDatabaseInTx()
        {
            await VerifyDatabaseNameOnSummaryTx(null, "neo4j");
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDefaultDatabaseWhenSpecifiedInTx()
        {
            await VerifyDatabaseNameOnSummaryTx("neo4j", "neo4j");
        }

        [RequireEnterpriseEdition("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDatabaseInTx()
        {
            await CreateDatabase(Server.Driver, "foo");
            try
            {
                await VerifyDatabaseNameOnSummaryTx("foo", "foo");
            }
            finally
            {
                await DropDatabase(Server.Driver, "foo");
            }
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDefaultDatabaseInTxFunc()
        {
            await VerifyDatabaseNameOnSummaryTxFunc(null, "neo4j");
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDefaultDatabaseWhenSpecifiedInTxFunc()
        {
            Console.WriteLine($"Version = {ServerVersion.From(BoltkitHelper.ServerVersion())}");
            await VerifyDatabaseNameOnSummaryTxFunc("neo4j", "neo4j");
        }

        [RequireEnterpriseEdition("4.0.0", GreaterThanOrEqualTo)]
        public async Task ShouldReturnDatabaseInfoForDatabaseInTxFunc()
        {
            await CreateDatabase(Server.Driver, "foo");
            try
            {
                await VerifyDatabaseNameOnSummaryTxFunc("foo", "foo");
            }
            finally
            {
                await DropDatabase(Server.Driver, "foo");
            }
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldThrowForNonExistentDatabase()
        {
            this.Awaiting(_ => VerifyDatabaseNameOnSummary("bar", "bar")).Should().Throw<ClientException>()
                .WithMessage("*database does not exist.*");
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldThrowForNonExistentDatabaseInTx()
        {
            this.Awaiting(_ => VerifyDatabaseNameOnSummaryTx("bar", "bar")).Should().Throw<ClientException>()
                .WithMessage("*database does not exist.*");
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldThrowForNonExistentDatabaseInTxFunc()
        {
            this.Awaiting(_ => VerifyDatabaseNameOnSummaryTxFunc("bar", "bar")).Should().Throw<ClientException>()
                .WithMessage("*database does not exist.*");
        }

        [RequireServerFact("4.0.0", LessThan)]
        public void ShouldThrowWhenDatabaseIsSpecified()
        {
            this.Awaiting(_ => VerifyDatabaseNameOnSummary("bar", "bar")).Should().Throw<ClientException>()
                .WithMessage("*to a server that does not support multiple databases.*");
        }

        [RequireServerFact("4.0.0", LessThan)]
        public void ShouldThrowWhenDatabaseIsSpecifiedInTx()
        {
            this.Awaiting(_ => VerifyDatabaseNameOnSummaryTx("bar", "bar")).Should().Throw<ClientException>()
                .WithMessage("*to a server that does not support multiple databases.*");
        }

        [RequireServerFact("4.0.0", LessThan)]
        public void ShouldThrowWhenDatabaseIsSpecifiedInTxFunc()
        {
            this.Awaiting(_ => VerifyDatabaseNameOnSummaryTxFunc("bar", "bar")).Should().Throw<ClientException>()
                .WithMessage("*to a server that does not support multiple databases.*");
        }

        private async Task VerifyDatabaseNameOnSummary(string name, string expected)
        {
            var session = Server.Driver.AsyncSession(o =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    o.WithDatabase(name);
                }
            });

            try
            {
                var cursor = await session.RunAsync("RETURN 1");
                var summary = await cursor.ConsumeAsync();

                summary.Database.Should().NotBeNull();
                summary.Database.Name.Should().Be(expected);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task VerifyDatabaseNameOnSummaryTx(string name, string expected)
        {
            var session = Server.Driver.AsyncSession(o =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    o.WithDatabase(name);
                }
            });

            try
            {
                var txc = await session.BeginTransactionAsync();
                var cursor = await txc.RunAsync("RETURN 1");
                var summary = await cursor.ConsumeAsync();

                summary.Database.Should().NotBeNull();
                summary.Database.Name.Should().Be(expected);

                await txc.CommitAsync();
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task VerifyDatabaseNameOnSummaryTxFunc(string name, string expected)
        {
            var session = Server.Driver.AsyncSession(o =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    o.WithDatabase(name);
                }
            });

            try
            {
                var summary = await session.ReadTransactionAsync(async txc =>
                {
                    var cursor = await txc.RunAsync("RETURN 1");
                    return await cursor.ConsumeAsync();
                });

                summary.Database.Should().NotBeNull();
                summary.Database.Name.Should().Be(expected);
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}