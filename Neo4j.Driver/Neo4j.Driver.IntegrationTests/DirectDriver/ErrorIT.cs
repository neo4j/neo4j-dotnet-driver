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
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class ErrorIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public ErrorIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task ErrorToRunSessionInTransaction()
        {
            var session = Driver.Session();
            try
            {
                var tx = await session.BeginTransactionAsync();
                try
                {
                    var ex = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

                    ex.Should().BeOfType<ClientException>().Which
                        .Message.Should().StartWith("Please close the currently open transaction object");
                }
                finally
                {
                    await tx.RollbackAsync();
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ErrorToRunTransactionInTransaction()
        {
            var session = Driver.Session();
            try
            {
                var tx = await session.BeginTransactionAsync();
                try
                {
                    var ex = await Record.ExceptionAsync(() => session.BeginTransactionAsync());

                    ex.Should().BeOfType<ClientException>().Which
                        .Message.Should().StartWith("Please close the currently open transaction object");
                }
                finally
                {
                    await tx.RollbackAsync();
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ErrorToRunInvalidCypher()
        {
            var session = Driver.Session();
            try
            {
                var result = await session.RunAsync("Invalid Cypher");
                var ex = await Record.ExceptionAsync(() => result.ConsumeAsync());

                ex.Should().BeOfType<ClientException>().Which
                    .Message.Should().StartWith("Invalid input");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldFailToConnectIncorrectPort()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:1234"))
            {
                var session = driver.Session();
                try
                {
                    var ex = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

                    ex.Should().BeOfType<ServiceUnavailableException>();
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public void ShouldReportWrongScheme()
        {
            var ex = Record.Exception(() => GraphDatabase.Driver("http://localhost"));

            ex.Should().BeOfType<NotSupportedException>().Which
                .Message.Should().Be("Unsupported URI scheme: http");
        }
    }
}