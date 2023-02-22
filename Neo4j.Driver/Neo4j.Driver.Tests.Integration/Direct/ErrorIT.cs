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
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class ErrorIT : DirectDriverTestBase
{
    public ErrorIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    private IDriver Driver => Server.Driver;

    [RequireServerFact]
    public async Task ErrorToRunSessionInTransaction()
    {
        await using var session = Driver.AsyncSession();
        var tx = await session.BeginTransactionAsync();
        try
        {
            var ex = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

            ex.Should()
                .BeOfType<TransactionNestingException>()
                .Which
                .Message.Should()
                .StartWith("Attempting to nest transactions");
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    [RequireServerFact]
    public async Task ErrorToRunTransactionInTransaction()
    {
        await using var session = Driver.AsyncSession();
        var tx = await session.BeginTransactionAsync();
        try
        {
            var ex = await Record.ExceptionAsync(() => session.BeginTransactionAsync());

            ex.Should()
                .BeOfType<TransactionNestingException>()
                .Which
                .Message.Should()
                .StartWith("Attempting to nest transactions");
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    [RequireServerFact]
    public async Task ErrorToRunInvalidCypher()
    {
        await using var session = Driver.AsyncSession();
        var result = await session.RunAsync("Invalid Cypher");
        var ex = await Record.ExceptionAsync(() => result.ConsumeAsync());

        ex.Should().BeOfType<ClientException>().Which.Code.Should().Be("Neo.ClientError.Statement.SyntaxError");
    }

    [RequireServerFact]
    public async Task ShouldFailToConnectIncorrectPort()
    {
        var uri = DefaultInstallation.BoltUri.Replace(DefaultInstallation.BoltPort, "1234");

        await using var driver = GraphDatabase.Driver(uri);
        await using var session = driver.AsyncSession();
        var ex = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

        ex.Should().BeOfType<ServiceUnavailableException>();
    }

    [RequireServerFact]
    public void ShouldReportWrongScheme()
    {
        var ex = Record.Exception(() => GraphDatabase.Driver("http://localhost"));

        ex.Should()
            .BeOfType<NotSupportedException>()
            .Which
            .Message.Should()
            .Be("Unsupported URI scheme: http");
    }
}
