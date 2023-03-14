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
using Microsoft.Reactive.Testing;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Reactive;
using Neo4j.Driver.TestUtil;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveTest;

namespace Neo4j.Driver.IntegrationTests.Stub;

public sealed class ResultStreamingTests
{
    private readonly ITestOutputHelper _output;
    private readonly Action<ConfigBuilder> _setupConfig;

    public ResultStreamingTests(ITestOutputHelper output)
    {
        _output = output;
        _setupConfig = o => o.WithLogger(TestLogger.Create(output));
    }

    [Fact]
    public async Task ShouldStreamingWithAsyncSession()
    {
        using var _ = BoltStubServer.Start("V4/streaming_records_all", 9001);
        await using var driver =
            GraphDatabase.Driver("bolt://127.0.0.1:9001", AuthTokens.None, _setupConfig);

        await using var session = driver.AsyncSession();
        var cursor =
            await session.RunAsync("MATCH (n) RETURN n.name");

        var result = await cursor.ToListAsync(r => r[0].As<string>());

        result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
    }

    [Fact]
    public async Task ShouldAllowChangeFetchSize()
    {
        using var _ = BoltStubServer.Start("V4/streaming_records", 9001);
        await using var driver = GraphDatabase.Driver(
            "bolt://127.0.0.1:9001",
            AuthTokens.None,
            o => o.WithLogger(TestLogger.Create(_output)).WithFetchSize(2));

        await using var session = driver.AsyncSession();
        var cursor =
            await session.RunAsync("MATCH (n) RETURN n.name");

        var result = await cursor.ToListAsync(r => r[0].As<string>());

        result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
    }

    [Fact]
    public void ShouldDiscardIfNotFinished()
    {
        using var _ = BoltStubServer.Start("V4/discard_streaming_records", 9001);
        using var driver = GraphDatabase.Driver(
            "bolt://127.0.0.1:9001",
            AuthTokens.None,
            o => o.WithLogger(TestLogger.Create(_output)).WithFetchSize(2));

        var session = driver.RxSession();

        session.Run("UNWIND [1,2,3,4] AS n RETURN n")
            .Keys()
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, Utils.MatchesKeys("n")),
                OnCompleted<string[]>(0));

        session.Close<string>().WaitForCompletion().AssertEqual(OnCompleted<string>(0));
    }

    [Fact]
    public void ShouldDiscardTxIfNotFinished()
    {
        using var _ = BoltStubServer.Start("V4/discard_streaming_records_tx", 9001);
        using var driver = GraphDatabase.Driver(
            "bolt://127.0.0.1:9001",
            AuthTokens.None,
            o => o.WithLogger(TestLogger.Create(_output)).WithFetchSize(2));

        var session = driver.RxSession();

        session.ExecuteRead(tx => tx.Run("UNWIND [1,2,3,4] AS n RETURN n").Keys())
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, Utils.MatchesKeys("n")),
                OnCompleted<string[]>(0));

        session.Close<string>().WaitForCompletion().AssertEqual(OnCompleted<string>(0));
    }
}
