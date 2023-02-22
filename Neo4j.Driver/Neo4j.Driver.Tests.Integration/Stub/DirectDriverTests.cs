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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.TestUtil;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stub;

public sealed class DirectDriverTests
{
    public DirectDriverTests(ITestOutputHelper output)
    {
        SetupConfig = o => o
            .WithEncryptionLevel(EncryptionLevel.None)
            .WithLogger(TestLogger.Create(output));
    }

    private Action<ConfigBuilder> SetupConfig { get; }

    [RequireBoltStubServerFact]
    public async Task ShouldLogServerAddress()
    {
        var logs = new List<string>();

        void SetupConfig(ConfigBuilder o)
        {
            o.WithEncryptionLevel(EncryptionLevel.None);
            o.WithLogger(new TestLogger(logs.Add, ExtendedLogLevel.Debug));
        }

        using var _ = BoltStubServer.Start("V4/accessmode_reader_implicit", 9001);
        await using var driver = GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, SetupConfig);
        await using var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        var cursor = await session.RunAsync("RETURN $x", new { x = 1 });
        var list = await cursor.ToListAsync(r => Convert.ToInt32(r[0]));

        list.Should().HaveCount(1).And.Contain(1);
        foreach (var log in logs.Where(log => log.StartsWith("[Debug]:[conn-")))
        {
            log.Should().Contain("127.0.0.1:9001");
        }
    }

    [RequireBoltStubServerFact]
    public async Task CanSendMultipleBookmarks()
    {
        var bookmarks = new[]
        {
            "neo4j:bookmark:v1:tx5", "neo4j:bookmark:v1:tx29",
            "neo4j:bookmark:v1:tx94", "neo4j:bookmark:v1:tx56",
            "neo4j:bookmark:v1:tx16", "neo4j:bookmark:v1:tx68"
        };

        using var _ = BoltStubServer.Start("V4/multiple_bookmarks", 9001);
        var uri = new Uri("bolt://127.0.0.1:9001");
        await using var driver = GraphDatabase.Driver(uri, SetupConfig);

        await using var session = driver.AsyncSession(o => o.WithBookmarks(Bookmarks.From(bookmarks)));
        try
        {
            var txc = await session.BeginTransactionAsync();
            try
            {
                await txc.RunAsync("CREATE (n {name:'Bob'})");
                await txc.CommitAsync();
            }
            catch
            {
                await txc.RollbackAsync();
                throw;
            }
        }
        catch (Exception)
        {
            // no op
        }

        session.LastBookmarks.Should().Be(Bookmarks.From("neo4j:bookmark:v1:tx95"));
    }

    [RequireBoltStubServerTheory]
    [InlineData("V3")]
    [InlineData("V4")]
    public async Task ShouldOnlyResetAfterError(string boltVersion)
    {
        using var _ = BoltStubServer.Start($"{boltVersion}/rollback_error", 9001);
        var uri = new Uri("bolt://127.0.0.1:9001");
        await using var driver = GraphDatabase.Driver(uri, SetupConfig);
        await using var session = driver.AsyncSession();
        var txc = await session.BeginTransactionAsync();
        try
        {
            var result = await txc.RunAsync("CREATE (n {name:'Alice'}) RETURN n.name AS name");
            var exception = await Record.ExceptionAsync(() => result.ConsumeAsync());

            exception.Should().BeOfType<TransientException>();
        }
        finally
        {
            await txc.RollbackAsync();
        }
    }

    [RequireBoltStubServerTheory]
    [InlineData("V3")]
    [InlineData("V4")]
    public async Task ShouldVerifyConnectivity(string boltVersion)
    {
        using var _ = BoltStubServer.Start($"{boltVersion}/supports_multidb", 9001);
        await using var driver = GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, SetupConfig);
        await driver.VerifyConnectivityAsync();
    }

    [RequireBoltStubServerTheory]
    [InlineData("V3")]
    [InlineData("V4")]
    public async Task ShouldThrowSecurityErrorWhenFailedToHello(string boltVersion)
    {
        using var _ = BoltStubServer.Start($"{boltVersion}/fail_to_auth", 9001);
        await using var driver = GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, SetupConfig);
        var error = await Record.ExceptionAsync(() => driver.VerifyConnectivityAsync());
        error.Should().BeOfType<AuthenticationException>().Which.Message.Should().StartWith("blabla");
    }

    [RequireBoltStubServerTheory]
    [InlineData("V4_1")] //Use same script on all versions after 4.1
    public async Task ShouldNotThrowOnNoopMessages(string boltVersion)
    {
        using var _ = BoltStubServer.Start($"{boltVersion}/noop", 9001);
        await using var driver = GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, SetupConfig);
        //await driver.VerifyConnectivityAsync();                    
        await using var session = driver.AsyncSession();
        await session.RunAsync("MATCH (N) RETURN n.name");
    }
}
