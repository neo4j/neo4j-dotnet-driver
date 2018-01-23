// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.SoakRunWorkItem;
using static Xunit.Record;

namespace Neo4j.Driver.IntegrationTests
{
    [Collection(CCIntegrationCollection.CollectionName)]
    public class RoutingDriverAsyncIT : IDisposable
    {
        public static readonly Config DebugConfig = Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Debug }).ToConfig();
        protected ITestOutputHelper Output { get; }
        protected CausalCluster Cluster { get; }
        protected IAuthToken AuthToken { get; }

        private string RoutingServer => Cluster.AnyCore().BoltRoutingUri.ToString();
        private string WrongServer => "bolt+routing://localhost:1234";

        public RoutingDriverAsyncIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
        {
            Output = output;
            Cluster = fixture.Cluster;
            AuthToken = Cluster.AuthToken;
        }

        public void Dispose()
        {
            // put some code that you want to run after each unit test
        }

        [RequireClusterFact]
        public async Task ShouldFailWithAuthenticationError()
        {
            Exception exception = null;
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthTokens.Basic("fake", "fake")))
            {
                var session = driver.Session();
                try
                {
                    exception = await ExceptionAsync(() => session.RunAsync("RETURN 1"));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
            exception.Should().BeOfType<AuthenticationException>();
            exception.Message.Should().Be("The client is unauthorized due to authentication failure.");
        }


        [RequireClusterFact]
        public async Task ShouldConnectClusterWithRoutingScheme()
        {
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthToken))
            {
                var session = driver.Session();
                try
                {
                    var result = await session.RunAsync("UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)");
                    var read = await result.FetchAsync();
                    read.Should().BeTrue();
                    result.Current[0].ValueAs<int>().Should().Be(10001 * 10000 / 2);
                    read = await result.FetchAsync();
                    read.Should().BeFalse();
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        [RequireClusterFact]
        public async Task ShouldThrowServiceUnavailableExceptionIfNoServer()
        {
            Exception error = null;
            using (var driver = GraphDatabase.Driver(WrongServer, AuthTokens.Basic("fake", "fake")))
            {
                var session = driver.Session();
                try
                {
                    error = await ExceptionAsync(() => session.RunAsync("RETURN 1"));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
            error.Should().BeOfType<ServiceUnavailableException>();
            error.Message.Should().Be("Failed to connect to any routing server. Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }

        [RequireClusterFact]
        public async Task ShouldDisallowMoreStatementAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
            var session = driver.Session();
            var result = await session.RunAsync("RETURN 1");
            await result.FetchAsync();
            result.Current[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            var error = await ExceptionAsync(() => session.RunAsync("RETURN 1"));
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().StartWith("Failed to acquire a new connection as the driver has already been disposed.");
        }

        [RequireClusterFact]
        public async Task ShouldDisallowMoreConnectionsAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
            var session = driver.Session();
            var result = await session.RunAsync("RETURN 1");
            await result.FetchAsync();
            result.Current[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            await session.CloseAsync();

            var error = Exception(() => driver.Session());
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }

        [RequireClusterTheory]
        [InlineData(50)]
        [InlineData(5000)]
        public async void SoakRunAsync(int threadCount)
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken, new Config
            {
                DriverMetricsEnabled = true,
                ConnectionTimeout = Config.InfiniteInterval,
                EncryptionLevel = EncryptionLevel.Encrypted,
                MaxIdleConnectionPoolSize = 50,
                MaxConnectionPoolSize = 50,
                ConnectionAcquisitionTimeout = TimeSpan.FromMinutes(5)
            });
            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var metrics = ((Internal.Driver) driver).GetDriverMetrics();
            var workItem = new SoakRunWorkItem(driver, metrics, Output);

            var tasks = new List<Task>();
            for (var i = 0; i < threadCount; i++)
            {
                tasks.Add(workItem.RunAsync());
            }
            await Task.WhenAll(tasks);

            var poolMetrics = metrics.PoolMetrics;
            Output.WriteLine(poolMetrics.ToContentString());
            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            foreach (var value in poolMetrics)
            {
                var st = value.Value;

                st.ToCreate.Should().Be(0);
                st.ToClose.Should().Be(0);
                st.InUse.Should().Be(0);
                st.Idle.Should().Be((int) (st.Created - st.Closed));
            }

            await driver.CloseAsync();
        }
    }
}
