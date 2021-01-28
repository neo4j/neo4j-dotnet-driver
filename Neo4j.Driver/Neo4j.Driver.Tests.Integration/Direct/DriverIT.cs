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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Metrics;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class DriverIT : DirectDriverTestBase
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
            var session = Server.Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync(
                    "CREATE (a {value: $value}) RETURN a.value", new Dictionary<string, object> {{"value", byteArray}});
                var value = await cursor.SingleAsync(r => r["a.value"].As<byte[]>());

                // Then
                value.Should().BeEquivalentTo(byteArray);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerWithIPv6Fact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldConnectIPv6AddressIfEnabled()
        {
            using (var driver = GraphDatabase.Driver("bolt://[::1]:7687", AuthToken,
                o => o.WithIpv6Enabled(true)))
            {
                var session = driver.AsyncSession();
                try
                {
                    var cursor = await session.RunAsync("RETURN 1");
                    var result = await cursor.SingleAsync(r => r[0].As<int>());

                    result.Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldNotConnectIPv6AddressIfDisabled()
        {
            using (var driver = GraphDatabase.Driver("bolt://[::1]:7687", AuthToken,
                o => o.WithIpv6Enabled(false)))
            {
                var session = driver.AsyncSession();
                try
                {
                    var exc = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

                    exc.GetBaseException().Should().BeOfType<NotSupportedException>();
                    exc.GetBaseException().Message.Should().Contain("This protocol version is not supported");
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerFact]
        public async Task ShouldConnectIPv4AddressIfIpv6Disabled()
        {
            var session = Server.Driver.AsyncSession();
            try
            {
                var cursor = await session.RunAsync("RETURN 1");
                var result = await cursor.SingleAsync(r => r[0].As<int>());

                result.Should().Be(1);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerWithIPv6Fact]
        public async Task ShouldConnectIPv4AddressIfIpv6Enabled()
        {
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken,
                o => o.WithIpv6Enabled(true)))
            {
                var session = driver.AsyncSession();
                try
                {
                    var cursor = await session.RunAsync("RETURN 1");
                    var result = await cursor.SingleAsync(r => r[0].As<int>());

                    result.Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireServerTheory]
        [InlineData(2)]
        [InlineData(10)]
        public async Task ShouldCloseAgedIdleConnections(int sessionCount)
        {
            // Given
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken, o=>
            {
                o.WithMetricsEnabled(true);
                o.WithConnectionIdleTimeout(TimeSpan.Zero); // enable but always timeout idle connections
            }))
            {
                // When
                for (var i = 0; i < sessionCount; i++)
                {
                    // should not reuse the same connection as it should timeout
                    var session = driver.AsyncSession();
                    try
                    {
                        var cursor = await session.RunAsync("RETURN 1");
                        var result = await cursor.SingleAsync(r => r[0].As<int>());

                        result.Should().Be(1);

                        Thread.Sleep(1); // block to let the timer aware the timeout
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }

                // Then
                var metrics = ((Internal.Driver) driver).GetMetrics();
                var m = metrics.ConnectionPoolMetrics.Single().Value;
                Output.WriteLine(m.ToString());
                m.Created.Should().Be(sessionCount);
                m.Created.Should().Be(m.Closed + 1);
            }
        }
    }
}