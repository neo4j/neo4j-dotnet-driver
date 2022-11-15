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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stub;

public class TransactionTests
{
    private static void NoEncryptionAndShortRetry(ConfigBuilder builder)
    {
        builder.WithEncryptionLevel(EncryptionLevel.None)
            .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(2));
    }

    public class ExplicitTransaction
    {
        [Theory]
        [InlineData("V3")]
        [InlineData("V4")]
        public void ShouldFailIfCommitFailsDueToBrokenConnection(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/connection_error_on_commit", 9001))
            {
                using (var driver =
                       GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, NoEncryptionAndShortRetry))
                {
                    using (var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
                    {
                        var txc = session.BeginTransaction();
                        var result = txc.Run("CREATE (n {name: 'Bob'})");

                        var exc = Record.Exception(() => txc.Commit());

                        exc.Should()
                            .BeOfType<ServiceUnavailableException>()
                            .Which
                            .HasCause<IOException>()
                            .Should()
                            .BeTrue();
                    }
                }
            }
        }

        [Theory]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task ShouldFailIfCommitFailsDueToBrokenConnectionAsync(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/connection_error_on_commit", 9001))
            {
                using (var driver =
                       GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, NoEncryptionAndShortRetry))
                {
                    var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
                    try
                    {
                        var txc = await session.BeginTransactionAsync();
                        var result = await txc.RunAsync("CREATE (n {name: 'Bob'})");

                        var exc = await Record.ExceptionAsync(() => txc.CommitAsync());

                        exc.Should()
                            .BeOfType<ServiceUnavailableException>()
                            .Which
                            .HasCause<IOException>()
                            .Should()
                            .BeTrue();
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }
        }
    }

    public class TransactionFunction
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TransactionFunction(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("V3")]
        [InlineData("V4")]
        public void ShouldFailIfCommitFailsDueToBrokenConnection(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/connection_error_on_commit", 9001))
            {
                using (var driver =
                       GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, NoEncryptionAndShortRetry))
                {
                    var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write));
                    try
                    {
                        var exc = Record.Exception(
                            () =>
                                session.WriteTransaction(txc => txc.Run("CREATE (n {name: 'Bob'})")));

                        exc.Should()
                            .BeOfType<ServiceUnavailableException>()
                            .Which
                            .HasCause<IOException>()
                            .Should()
                            .BeTrue();
                    }
                    finally
                    {
                        session.Dispose();
                    }
                }
            }
        }

        [Theory]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task ShouldFailIfCommitFailsDueToBrokenConnectionAsync(string boltVersion)
        {
            using (BoltStubServer.Start($"{boltVersion}/connection_error_on_commit", 9001))
            {
                using (var driver =
                       GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, NoEncryptionAndShortRetry))
                {
                    var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

                    try
                    {
                        var exc = await Record.ExceptionAsync(
                            () =>
                            {
                                return session.WriteTransactionAsync(txc => txc.RunAsync("CREATE (n {name: 'Bob'})"));
                            });

                        exc.Should()
                            .BeOfType<ServiceUnavailableException>()
                            .Which
                            .HasCause<IOException>()
                            .Should()
                            .BeTrue();
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
