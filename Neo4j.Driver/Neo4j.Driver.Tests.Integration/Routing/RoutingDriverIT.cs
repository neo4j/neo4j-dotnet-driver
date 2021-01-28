﻿// Copyright (c) "Neo4j"
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
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Routing
{
    [Collection(CCIntegrationCollection.CollectionName)]
    public class RoutingDriverIT : RoutingDriverTestBase
    {
        public RoutingDriverIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture) : base(output,
            fixture)
        {
        }

        [RequireClusterFact]
        public async Task ShouldFailWithAuthenticationError()
        {
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthTokens.Basic("fake", "fake")))
            {
                var session = driver.AsyncSession();
                try
                {
                    var exc = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

                    exc.Should().BeOfType<AuthenticationException>().Which
                        .Message.Should().Be("The client is unauthorized due to authentication failure.");
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }


        [RequireClusterFact]
        public async Task ShouldConnectClusterWithRoutingScheme()
        {
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthToken))
            {
                var session = driver.AsyncSession();
                try
                {
                    var result = await session.RunAndSingleAsync("UNWIND range(1,10000) AS x RETURN sum(x)", null,
                        r => r[0].As<int>());

                    result.Should().Be(10001 * 10000 / 2);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireClusterFact]
        public async Task ShouldLoadBalanceBetweenServers()
        {
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthToken))
            {
                for (var i = 0; i < 10; i++)
                {
                    string addr1, addr2;

                    var session1 = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
                    try
                    {
                        var summary = await session1.RunAndConsumeAsync("RETURN 1");
                        addr1 = summary.Server.Address;
                    }
                    finally
                    {
                        await session1.CloseAsync();
                    }

                    var session2 = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
                    try
                    {
                        var summary = await session2.RunAndConsumeAsync("RETURN 2");
                        addr2 = summary.Server.Address;
                    }
                    finally
                    {
                        await session2.CloseAsync();
                    }

                    addr1.Should().NotBe(addr2);
                }
            }
        }

        [RequireClusterFact]
        public async Task ShouldThrowServiceUnavailableExceptionIfNoServer()
        {
            using (var driver = GraphDatabase.Driver(WrongServer, AuthTokens.Basic("fake", "fake")))
            {
                var session = driver.AsyncSession();
                try
                {
                    var exc = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

                    exc.Should().BeOfType<ServiceUnavailableException>().Which
                        .Message.Should()
                        .Be(
                            "Failed to connect to any routing server. Please make sure that the cluster is up and can be accessed by the driver and retry.");
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireClusterFact]
        public async Task ShouldDisallowMoreQueryAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken);

            var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
            var result = await session.RunAndSingleAsync("RETURN 1", null, r => r[0].As<int>());
            result.Should().Be(1);

            driver.Dispose();

            var error = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));
            error.Should().BeOfType<ObjectDisposedException>().Which
                .Message.Should()
                .StartWith("Failed to acquire a new connection as the driver has already been disposed.");
        }

        [RequireClusterFact]
        public async Task ShouldDisallowMoreConnectionsAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken);

            var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
            var result = await session.RunAndSingleAsync("RETURN 1", null, r => r[0].As<int>());
            result.Should().Be(1);

            driver.Dispose();

            await session.CloseAsync();

            var error = Record.Exception(() => driver.AsyncSession());
            error.Should().BeOfType<ObjectDisposedException>().Which
                .Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }
    }
}