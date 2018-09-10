// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    [Collection(CCIntegrationCollection.CollectionName)]
    public class RoutingDriverIT : RoutingDriverTestBase
    {
        public RoutingDriverIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireClusterFact]
        public void ShouldFailWithAuthenticationError()
        {
            Exception exception = null;
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthTokens.Basic("fake", "fake")))
            using(var session = driver.Session())
            {
                exception = Record.Exception(() => session.Run("RETURN 1"));
            }
            exception.Should().BeOfType<AuthenticationException>();
            exception.Message.Should().Be("The client is unauthorized due to authentication failure.");
        }


        [RequireClusterFact]
        public void ShouldConnectClusterWithRoutingScheme()
        {
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthToken))
            using (var session = driver.Session())
            {
                var result = session.Run("UNWIND range(1,10000) AS x RETURN sum(x)");
                result.Single()[0].ValueAs<int>().Should().Be(10001 * 10000 / 2);
            }
        }

        [RequireClusterFact]
        public void ShouldLoadBalanceBetweenServers()
        {
            using (var driver = GraphDatabase.Driver(RoutingServer, AuthToken))
            {
                string addr1, addr2;
                for (int i = 0; i < 10; i++)
                {
                    using (var session = driver.Session(AccessMode.Read))
                    {
                        var result = session.Run("RETURN 1");
                        addr1 = result.Summary.Server.Address;
                    }
                    using (var session = driver.Session(AccessMode.Read))
                    {
                        addr2 = session.Run("RETURN 2").Summary.Server.Address;
                    }
                    addr1.Should().NotBe(addr2);
                }
            }
        }

        [RequireClusterFact]
        public void ShouldThrowServiceUnavailableExceptionIfNoServer()
        {
            Exception error = null;
            using (var driver = GraphDatabase.Driver(WrongServer, AuthTokens.Basic("fake", "fake")))
            using (var session = driver.Session())
            {
                error = Record.Exception(() => session.Run("RETURN 1"));
            }
            error.Should().BeOfType<ServiceUnavailableException>();
            error.Message.Should().Be("Failed to connect to any routing server. Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }

        [RequireClusterFact]
        public void ShouldDisallowMoreStatementAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
            var session = driver.Session(AccessMode.Write);
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            var error = Record.Exception(() => session.Run("RETURN 1"));
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().StartWith("Failed to acquire a new connection as the driver has already been disposed.");
        }

        [RequireClusterFact]
        public void ShouldDisallowMoreConnectionsAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken);
            var session = driver.Session(AccessMode.Write);
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            session.Dispose();

            var error = Record.Exception(() => driver.Session());
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }

        [RequireClusterTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SoakRunTests(int threadCount)
        {
            var driver = GraphDatabase.Driver(RoutingServer, AuthToken, new Config
            {
                MetricsFactory = new DefaultMetricsFactory(),
                ConnectionTimeout = Config.InfiniteInterval,
                EncryptionLevel = EncryptionLevel.Encrypted,
                MaxConnectionPoolSize = 100,
                ConnectionAcquisitionTimeout = TimeSpan.FromMinutes(2)
            });
            try
            {
                var startTime = DateTime.Now;
                Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

                var metrics = ((Internal.Driver) driver).GetMetrics();
                var workItem = new SoakRunWorkItem(driver, metrics, Output);

                Parallel.For(0, threadCount, workItem.Run);

                var poolMetrics = metrics.ConnectionPoolMetrics;
                Output.WriteLine(poolMetrics.ToContentString());
                var endTime = DateTime.Now;
                Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
                Output.WriteLine($"Total time spent: {endTime - startTime}");

                foreach (var value in poolMetrics)
                {
                    var st = value.Value;

                    st.Creating.Should().Be(0);
                    st.Closing.Should().Be(0);
                    st.InUse.Should().Be(0);
                    st.Idle.Should().Be((int) (st.Created - st.Closed + st.FailedToCreate));
                }
            }
            finally
            {
                driver.Close();
            }
        }
    }
}
