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
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stub
{
    public class RoutingDriverTests
    {
        public Action<ConfigBuilder> SetupConfig { get; }

        public RoutingDriverTests(ITestOutputHelper output)
        {
            SetupConfig = o => o
                .WithEncryptionLevel(EncryptionLevel.None);
        }

        [RequireBoltStubServerTheory]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task SendRoutingContextToServer(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/get_routing_table_with_context", 9010))
            {
                var uri = new Uri("neo4j://127.0.0.1:9010/?policy=my_policy&region=china");
                using (var driver = GraphDatabase.Driver(uri, SetupConfig))
                {
                    var session = driver.AsyncSession();
                    try
                    {
                        var cursor = await session.RunAsync("MATCH (n) RETURN n.name AS name");
                        var records = await cursor.ToListAsync();

                        records.Count.Should().Be(2);
                        records[0]["name"].As<string>().Should().Be("Alice");
                        records[1]["name"].As<string>().Should().Be("Bob");
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }
        }

        [RequireBoltStubServerTheory]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task InvokeProcedureGetRoutingTableWhenServerVersionPermits(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/get_routing_table", 9010))
            {
                var uri = new Uri("neo4j://127.0.0.1:9010");
                using (var driver = GraphDatabase.Driver(uri, SetupConfig))
                {
                    var session = driver.AsyncSession();
                    try
                    {
                        var cursor = await session.RunAsync("MATCH (n) RETURN n.name AS name");
                        var records = await cursor.ToListAsync();

                        records.Count.Should().Be(3);
                        records[0]["name"].As<string>().Should().Be("Alice");
                        records[1]["name"].As<string>().Should().Be("Bob");
                        records[2]["name"].As<string>().Should().Be("Eve");
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }
        }

        [RequireBoltStubServerTheory]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task ShouldVerifyConnectivity(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/verify_connectivity", 9010))
            {
                using (var driver = GraphDatabase.Driver("neo4j://127.0.0.1:9010", AuthTokens.None, SetupConfig))
                {
                    await driver.VerifyConnectivityAsync();
                }
            }
        }

        [RequireBoltStubServerTheory]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task ShouldThrowSecurityErrorWhenFailedToHello(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/fail_to_auth", 9001))
            {
                using (var driver = GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, SetupConfig))
                {
                    var error = await Record.ExceptionAsync(() => driver.VerifyConnectivityAsync());
                    error.Should().BeOfType<AuthenticationException>();
                    error.Message.Should().StartWith("blabla");
                }
            }
        }
    }
}