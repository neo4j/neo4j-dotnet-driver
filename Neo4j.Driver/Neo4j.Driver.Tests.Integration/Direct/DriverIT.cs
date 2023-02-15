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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal.IO;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class DriverIT : DirectDriverTestBase
{
    public DriverIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    [RequireServerFact("3.2.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task ShouldPackAndUnpackBytes()
    {
        // Given
        var byteArray = PackStreamBitConverter.GetBytes("hello, world");

        // When
        await using var session = Server.Driver.AsyncSession();
        var cursor = await session.RunAsync(
            "CREATE (a {value: $value}) RETURN a.value",
            new Dictionary<string, object> { { "value", byteArray } });

        var value = await cursor.SingleAsync(r => r["a.value"].As<byte[]>());

        // Then
        value.Should().BeEquivalentTo(byteArray);
    }

    [RequireServerWithIPv6Fact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task ShouldConnectIPv6AddressIfEnabled()
    {
        await using var driver = GraphDatabase.Driver(
            "bolt://[::1]:7687",
            AuthToken,
            o => o.WithIpv6Enabled(true));

        await using var session = driver.AsyncSession();
        var cursor = await session.RunAsync("RETURN 1");
        var result = await cursor.SingleAsync(r => r[0].As<int>());

        result.Should().Be(1);
    }

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task ShouldNotConnectIPv6AddressIfDisabled()
    {
        await using var driver = GraphDatabase.Driver(
            "bolt://[::1]:7687",
            AuthToken,
            o => o.WithIpv6Enabled(false));

        await using var session = driver.AsyncSession();
        var exc = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

        exc.Should().NotBeNull();
        exc!.GetBaseException().Should().BeOfType<NotSupportedException>();
        exc.GetBaseException().Message.Should().Contain("This protocol version is not supported");
    }

    [RequireServerFact]
    public async Task ShouldConnectIPv4AddressIfIpv6Disabled()
    {
        await using var session = Server.Driver.AsyncSession();
        var cursor = await session.RunAsync("RETURN 1");
        var result = await cursor.SingleAsync(r => r[0].As<int>());

        result.Should().Be(1);
    }

    [RequireServerWithIPv6Fact]
    public async Task ShouldConnectIPv4AddressIfIpv6Enabled()
    {
        await using var driver = GraphDatabase.Driver(
            DefaultInstallation.BoltUri,
            AuthToken,
            o => o.WithIpv6Enabled(true));

        await using var session = driver.AsyncSession();
        var cursor = await session.RunAsync("RETURN 1");
        var result = await cursor.SingleAsync(r => r[0].As<int>());

        result.Should().Be(1);
    }

    [RequireServerTheory]
    [InlineData(2)]
    [InlineData(10)]
    public async Task ShouldCloseAgedIdleConnections(int sessionCount)
    {
        // Given
        await using var driver = GraphDatabase.Driver(
            DefaultInstallation.BoltUri,
            AuthToken,
            o =>
            {
                o.WithMetricsEnabled(true);
                o.WithConnectionIdleTimeout(TimeSpan.Zero); // enable but always timeout idle connections
            });

        // When
        for (var i = 0; i < sessionCount; i++)
        {
            // should not reuse the same connection as it should timeout
            await using var session = driver.AsyncSession();
            var cursor = await session.RunAsync("RETURN 1");
            var result = await cursor.SingleAsync(r => r[0].As<int>());

            result.Should().Be(1);

            Thread.Sleep(1); // block to let the timer aware the timeout
        }

        // Then
        var metrics = ((Internal.Driver)driver).GetMetrics();
        var m = metrics.ConnectionPoolMetrics.Single().Value;
        Output.WriteLine(m.ToString());
        m.Created.Should().Be(sessionCount);
        m.Created.Should().Be(m.Closed + 1);
    }
}
