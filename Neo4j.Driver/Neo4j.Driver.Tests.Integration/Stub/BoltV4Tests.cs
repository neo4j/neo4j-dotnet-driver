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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.IntegrationTests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stub
{
    public class BoltV4Tests
    {
        private readonly ITestOutputHelper _output;
        private readonly Config _config;

        public BoltV4Tests(ITestOutputHelper output)
        {
            _output = output;

            _config = Config.Builder.WithDriverLogger(TestDriverLogger.Create(output)).ToConfig();
        }

        [Fact]
        public async Task ShouldDiscoverEndpointsForADatabaseAndRead()
        {
            using (BoltStubServer.Start("V4/acquire_endpoints_aDatabase", 9001))
            {
                using (BoltStubServer.Start("V4/read_from_aDatabase", 9005))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, _config))
                    {
                        var session = driver.AsyncSession(o =>
                            o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read));
                        try
                        {
                            var cursor =
                                await session.RunAsync("MATCH (n) RETURN n.name");
                            var result = await cursor.ToListAsync(r => r[0].As<string>());

                            result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task ShouldDiscoverEndpointsForADatabaseAndWrite()
        {
            using (BoltStubServer.Start("V4/acquire_endpoints_aDatabase", 9001))
            {
                using (BoltStubServer.Start("V4/write_to_aDatabase", 9007))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, _config))
                    {
                        var session = driver.AsyncSession(o =>
                            o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Write));
                        try
                        {
                            await session.RunAndConsumeAsync("CREATE (n {name:'Bob'})");
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }


        [Fact]
        public async Task ShouldDiscoverEndpointsForDefaultDatabase()
        {
            using (BoltStubServer.Start("V4/acquire_endpoints_default_database", 9001))
            {
                using (BoltStubServer.Start("V4/read", 9005))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, _config))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
                        try
                        {
                            var cursor =
                                await session.RunAsync("MATCH (n) RETURN n.name");
                            var result = await cursor.ToListAsync(r => r[0].As<string>());

                            result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [Fact]
        public void ShouldThrowOnInvalidRoutingTable()
        {
            using (BoltStubServer.Start("V4/acquire_endpoints_aDatabase_no_servers", 9001))
            {
                using (var driver =
                    GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, _config))
                {
                    this.Awaiting(async _ =>
                        {
                            var session = driver.AsyncSession(o =>
                                o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read));
                            try
                            {
                                var cursor =
                                    await session.RunAsync("MATCH (n) RETURN n.name");
                                var result = await cursor.ToListAsync(r => r[0].As<string>());

                                result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
                            }
                            finally
                            {
                                await session.CloseAsync();
                            }
                        })
                        .Should()
                        .Throw<ServiceUnavailableException>()
                        .WithMessage("Failed to connect to any routing server.*");
                }
            }
        }

        [Fact]
        public void ShouldThrowOnProcedureNotFound()
        {
            using (BoltStubServer.Start("V4/acquire_endpoints_db_not_found", 9001))
            {
                using (var driver =
                    GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, _config))
                {
                    this.Awaiting(async _ =>
                        {
                            var session = driver.AsyncSession(o =>
                                o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read));
                            try
                            {
                                var cursor =
                                    await session.RunAsync("MATCH (n) RETURN n.name");
                                var result = await cursor.ToListAsync(r => r[0].As<string>());

                                result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
                            }
                            finally
                            {
                                await session.CloseAsync();
                            }
                        })
                        .Should()
                        .Throw<FatalDiscoveryException>()
                        .WithMessage("database not found");
                }
            }
        }

        [Fact]
        public async Task ShouldDiscoverEndpointsForADatabaseWithBookmarks()
        {
            using (BoltStubServer.Start("V4/acquire_endpoints_aDatabase_with_bookmark", 9001))
            {
                using (BoltStubServer.Start("V4/read_from_aDatabase_with_bookmark", 9005))
                {
                    var bookmark1 = Bookmark.From("system:1111");
                    var bookmark2 = Bookmark.From("aDatabase:5555");

                    using (var driver =
                        GraphDatabase.Driver("neo4j://localhost:9001", AuthTokens.None, _config))
                    {
                        var session = driver.AsyncSession(o =>
                            o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read)
                                .WithBookmarks(bookmark1, bookmark2));
                        try
                        {
                            var cursor =
                                await session.RunAsync("MATCH (n) RETURN n.name");
                            var result = await cursor.ToListAsync(r => r[0].As<string>());

                            result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }
    }
}