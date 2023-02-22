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

namespace Neo4j.Driver.IntegrationTests.Routing;

public sealed class RoutingDriverAsyncIT : RoutingDriverTestBase
{
    public RoutingDriverAsyncIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture) : base(
        output,
        fixture)
    {
    }

    [RequireClusterFact]
    public async Task ShouldFailWithAuthenticationError()
    {
        await using var driver = GraphDatabase.Driver(RoutingServer, AuthTokens.Basic("fake", "fake"));
        await using var session = driver.AsyncSession();
        var exception = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

        exception.Should()
            .BeOfType<AuthenticationException>()
            .Which.Message.Should()
            .Be("The client is unauthorized due to authentication failure.");
    }

    [RequireClusterFact]
    public async Task ShouldConnectClusterWithRoutingScheme()
    {
        await using var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
        await using var session = driver.AsyncSession();
        var result = await session.RunAsync("UNWIND range(1,10000) AS x RETURN sum(x)");
        var read = await result.FetchAsync();
        read.Should().BeTrue();
        result.Current[0].As<int>().Should().Be(10001 * 10000 / 2);
        read = await result.FetchAsync();
        read.Should().BeFalse();
    }

    [RequireClusterFact]
    public async Task ShouldThrowServiceUnavailableExceptionIfNoServer()
    {
        await using var driver = GraphDatabase.Driver(WrongServer, AuthTokens.Basic("fake", "fake"));
        await using var session = driver.AsyncSession();
        var error = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

        error.Should()
            .BeOfType<ServiceUnavailableException>()
            .Which.Message.Should()
            .Be(
                "Failed to connect to any routing server. Please make sure that the cluster is up and can be accessed by the driver and retry.");
    }

    [RequireClusterFact]
    public async Task ShouldDisallowMoreQueryAfterDriverDispose()
    {
        var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
        var session = driver.AsyncSession();
        var result = await session.RunAsync("RETURN 1");
        await result.FetchAsync();
        result.Current[0].As<int>().Should().Be(1);

        driver.Dispose();
        var error = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));
        error.Should()
            .BeOfType<ObjectDisposedException>()
            .Which.Message.Should()
            .StartWith("Failed to acquire a new connection as the driver has already been disposed.");
    }

    [RequireClusterFact]
    public async Task ShouldDisallowMoreConnectionsAfterDriverDispose()
    {
        var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
        var session = driver.AsyncSession();
        var result = await session.RunAsync("RETURN 1");
        await result.FetchAsync();
        result.Current[0].As<int>().Should().Be(1);

        driver.Dispose();
        await session.CloseAsync();

        var error = Record.Exception(() => driver.AsyncSession());
        error.Should()
            .BeOfType<ObjectDisposedException>()
            .Which.Message.Should()
            .Contain("Cannot open a new session on a driver that is already disposed.");
    }
}
