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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Shared;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class DriverIT : DirectDriverTestBase
    {
        public DriverIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.2.0")]
        public void ShouldPackAndUnpackBytes()
        {
            // Given
            byte[] byteArray = PackStreamBitConverter.GetBytes("hello, world");

            // When
            using (var session = Server.Driver.Session())
            {
                var result = session.Run(
                    "CREATE (a {value:{value}}) RETURN a.value", new Dictionary<string, object> {{"value", byteArray}});
                // Then
                foreach (var record in result)
                {
                    var value = record["a.value"].ValueAs<byte[]>();
                    value.Should().BeEquivalentTo(byteArray);
                }
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldConnectIPv6AddressIfEnabled()
        {
            using (var driver = GraphDatabase.Driver("bolt://[::1]:7687", AuthToken, new Config { Ipv6Enabled = true }))
            using (var session = driver.Session())
            {
                var ret = session.Run("RETURN 1").Single();
                ret[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldNotConnectIPv6AddressIfDisabled()
        {
            using (var driver = GraphDatabase.Driver("bolt://[::1]:7687", AuthToken))
            using (var session = driver.Session())
            {
                var exception = Record.Exception(() => session.Run("RETURN 1"));
                exception.GetBaseException().Should().BeOfType<NotSupportedException>();
                exception.GetBaseException().Message.Should().Contain("This protocol version is not supported");
            }
        }

        [RequireServerFact]
        public void ShouldConnectIPv4AddressIfIpv6Disabled()
        {
            using (var session = Server.Driver.Session())
            {
                var ret = session.Run("RETURN 1").Single();
                ret[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerFact]
        public void ShouldConnectIPv4AddressIfIpv6Enabled()
        {
            using (
                var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken, new Config {Ipv6Enabled = true}))
            using (var session = driver.Session())
            {
                var ret = session.Run("RETURN 1").Single();
                ret[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerTheory]
        [InlineData(2)]
        [InlineData(10)]
        public void ShouldCloseIdleForTooLongConns(int sessionCount)
        {
            // Given
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken, new Config
            {
                MetricsFactory = new DefaultMetricsFactory(),
                ConnectionIdleTimeout = TimeSpan.Zero // enable but always timeout idle connections
            }))
            {
                // When
                for (var i = 0; i < sessionCount; i++)
                {
                    // should not reuse the same connection as it should timeout
                    using (var session = driver.Session())
                    {
                        var ret = session.Run("RETURN 1").Single();
                        ret[0].ValueAs<int>().Should().Be(1);
                        Thread.Sleep(1); // block to let the timer aware the timeout
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

        [RequireServerTheory]
        [InlineData(50)]
        [InlineData(5000)]
//        [InlineData(50000)] leave this to a long dedicated build
        public void SoakRun(int threadCount)
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, new Config
            {
                MetricsFactory = new DefaultMetricsFactory(),
                ConnectionTimeout = Config.InfiniteInterval,
                EncryptionLevel = EncryptionLevel.Encrypted,
                MaxConnectionPoolSize = 100
            });


            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var metrics = ((Internal.Driver) driver).GetMetrics();
            var workItem = new SoakRunWorkItem(driver, metrics, Output);

            Parallel.For(0, threadCount, workItem.Run);

            var m = metrics.ConnectionPoolMetrics.Single().Value;
            var cm = metrics.ConnectionMetrics.Single().Value;
            Output.WriteLine(m.ToString());
            Output.WriteLine(m.AcquisitionTimeHistogram.ToString());
            Output.WriteLine(cm.ConnectionTimeHistogram.ToString());
            Output.WriteLine(cm.InUseTimeHistogram.ToString());

            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            m.Creating.Should().Be(0);
            m.Closing.Should().Be(0);
            m.InUse.Should().Be(0);
            m.Idle.Should().Be((int) (m.Created - m.Closed + m.FailedToCreate));

            driver.Close();
        }

        [RequireServerTheory]
        [InlineData(50)]
        [InlineData(5000)]
        public async void SoakRunAsync(int threadCount)
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, new Config
            {
                MetricsFactory = new DefaultMetricsFactory(),
                ConnectionTimeout = Config.InfiniteInterval,
                MaxConnectionPoolSize = 100,
                EncryptionLevel = EncryptionLevel.Encrypted,
                ConnectionAcquisitionTimeout = TimeSpan.FromMinutes(2)
            });

            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var metrics = ((Internal.Driver) driver).GetMetrics();
            var workItem = new SoakRunWorkItem(driver, metrics, Output);

            var tasks = new List<Task>();
            for (var i = 0; i < threadCount; i++)
            {
                tasks.Add(workItem.RunAsync());
            }
            await Task.WhenAll(tasks);

            var m = metrics.ConnectionPoolMetrics.Single().Value;
            Output.WriteLine(m.ToString());
            Output.WriteLine(m.AcquisitionTimeHistogram.ToString());

            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            m.Creating.Should().Be(0);
            m.Closing.Should().Be(0);
            m.InUse.Should().Be(0);
            m.Idle.Should().Be((int) (m.Created - m.Closed + m.FailedToCreate));

            await driver.CloseAsync();
        }
    }
}
