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

using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Stub
{
    public class AccessModeTests
    {
        private static void NoEncryption(ConfigBuilder builder)
        {
            builder.WithEncryptionLevel(EncryptionLevel.None);
        }

        [RequireBoltStubServerFact]
        public async Task RunOnReadModeSessionShouldGoToReader()
        {
            using (BoltStubServer.Start("V4/accessmode_router", 9001))
            {
                using (BoltStubServer.Start("V4/accessmode_reader_implicit", 9003))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, NoEncryption))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
                        try
                        {
                            var result = await session.RunAndSingleAsync("RETURN $x", new {x = 1}, r => r[0].As<int>());

                            result.Should().Be(1);
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task RunOnReadModeTransactionShouldGoToReader()
        {
            using (BoltStubServer.Start("V4/accessmode_router", 9001))
            {
                using (BoltStubServer.Start("V4/accessmode_reader_explicit", 9003))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, NoEncryption))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
                        try
                        {
                            var tx = await session.BeginTransactionAsync();
                            var result = await tx.RunAndSingleAsync("RETURN $x", new {x = 1}, r => r[0].As<int>());

                            result.Should().Be(1);

                            await tx.CommitAsync();
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerTheory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task ReadTransactionOnSessionShouldGoToReader(AccessMode mode)
        {
            using (BoltStubServer.Start("V4/accessmode_router", 9001))
            {
                using (BoltStubServer.Start("V4/accessmode_reader_func", 9003))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, NoEncryption))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(mode));
                        try
                        {
                            var result = await session.ReadTransactionAsync(tx =>
                                tx.RunAndSingleAsync("RETURN $x", new {x = 1}, r => r[0].As<int>()));

                            result.Should().Be(1);
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task RunOnWriteModeSessionShouldGoToWriter()
        {
            using (BoltStubServer.Start("V4/accessmode_router", 9001))
            {
                using (BoltStubServer.Start("V4/accessmode_writer_implicit", 9002))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, NoEncryption))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
                        try
                        {
                            var result = await session.RunAndSingleAsync("CREATE (n: { id: $x }) RETURN $x",
                                new {x = 1}, r => r[0].As<int>());

                            result.Should().Be(1);
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task RunOnWriteModeTransactionShouldGoToWriter()
        {
            using (BoltStubServer.Start("V4/accessmode_router", 9001))
            {
                using (BoltStubServer.Start("V4/accessmode_writer_explicit", 9002))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, NoEncryption))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
                        try
                        {
                            var tx = await session.BeginTransactionAsync();

                            var result = await tx.RunAndSingleAsync("CREATE (n: { id: $x }) RETURN $x", new {x = 1},
                                r => r[0].As<int>());

                            result.Should().Be(1);

                            await tx.CommitAsync();
                        }
                        finally
                        {
                            await session.CloseAsync();
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerTheory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task WriteTransactionOnSessionShouldGoToWriter(AccessMode mode)
        {
            using (BoltStubServer.Start("V4/accessmode_router", 9001))
            {
                using (BoltStubServer.Start("V4/accessmode_writer_func", 9002))
                {
                    using (var driver =
                        GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, NoEncryption))
                    {
                        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(mode));
                        try
                        {
                            var result = await session.WriteTransactionAsync(tx =>
                                tx.RunAndSingleAsync("CREATE (n: { id: $x }) RETURN $x", new {x = 1},
                                    r => r[0].As<int>()));

                            result.Should().Be(1);
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