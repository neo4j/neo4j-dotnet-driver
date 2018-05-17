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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class DriverIT : DirectDriverIT
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
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken))
            using (var session = driver.Session())
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

        [RequireServerVersionLessThanFact("3.2.0")]
        public void ShouldNotPackBytes()
        {
            // Given
            byte[] byteArray = PackStreamBitConverter.GetBytes("hello, world");

            // When
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken))
            using (var session = driver.Session())
            {
                var exception = Record.Exception(() =>
                            session.Run("CREATE (a {value:{value}})",
                                new Dictionary<string, object> {{"value", byteArray}}));

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Cannot understand values with type System.Byte[]");
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldConnectIPv6AddressIfEnabled()
        {
            using (var driver = GraphDatabase.Driver("bolt://[::1]:7687", AuthToken, new Config {Ipv6Enabled = true}))
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
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken))
            using (var session = driver.Session())
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
            var statisticsCollector = new StatisticsCollector();
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken, new Config
            {
                DriverStatisticsCollector = statisticsCollector,
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
                var statistics = statisticsCollector.CollectStatistics().Single();
                var st = ConnectionPoolStatistics.FromDictionary(statistics.Key, statistics.Value.ValueAs<IDictionary<string, object>>());
                Output.WriteLine(st.ReportStatistics().ToContentString());
                st.ConnCreated.Should().Be(sessionCount);
                st.ConnCreated.Should().Be(st.ConnClosed + 1);
            }
        }

        [RequireServerTheory]
        [InlineData(50)]
        [InlineData(5000)]
        //        [InlineData(50000)] leave this to a long dedicated build
        public void SoakRun(int threadCount)
        {
            var statisticsCollector = new StatisticsCollector();
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, new Config
            {
                DriverStatisticsCollector = statisticsCollector,
                ConnectionTimeout = Config.InfiniteInterval,
                EncryptionLevel = EncryptionLevel.Encrypted
            });


            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var workItem = new SoakRunWorkItem(driver, statisticsCollector, Output);

            var tasks = new List<Task>();
            for (var i = 0; i < threadCount; i++)
            {
                tasks.Add(workItem.Run());
            }
            Task.WaitAll(tasks.ToArray());

            driver.Close();

            var statistics = statisticsCollector.CollectStatistics().Single();
            var st = ConnectionPoolStatistics.FromDictionary(statistics.Key, statistics.Value as IDictionary<string, object>);
            Output.WriteLine(st.ReportStatistics().ToContentString());
            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            st.ConnToCreate.Should().Be(st.ConnCreated + st.ConnFailedToCreate);
            st.ConnToCreate.Should().Be(st.InUseConns + st.AvailableConns + st.ConnToClose);
            st.ConnToClose.Should().Be(st.ConnClosed);
        }

        [RequireServerTheory]
        [InlineData(50)]
        [InlineData(5000)]
        public async void SoakRunAsync(int threadCount)
        {
            var statisticsCollector = new StatisticsCollector();
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, new Config
            {
                DriverStatisticsCollector = statisticsCollector,
                ConnectionTimeout = Config.InfiniteInterval,
                MaxConnectionPoolSize = 500,
                EncryptionLevel = EncryptionLevel.Encrypted,
                ConnectionAcquisitionTimeout = TimeSpan.FromMinutes(2)
            });

            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var workItem = new SoakRunWorkItem(driver, statisticsCollector, Output);

            var tasks = new List<Task>();
            for (var i = 0; i < threadCount; i++)
            {
                tasks.Add(workItem.RunAsync());
            }
            await Task.WhenAll(tasks);

            await driver.CloseAsync();

            var statistics = statisticsCollector.CollectStatistics().Single();
            var st = ConnectionPoolStatistics.FromDictionary(statistics.Key, statistics.Value as IDictionary<string, object>);
            Output.WriteLine(st.ReportStatistics().ToContentString());
            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            st.ConnToCreate.Should().Be(st.ConnCreated + st.ConnFailedToCreate);
            st.ConnToCreate.Should().Be(st.InUseConns + st.AvailableConns + st.ConnToClose);
            st.ConnToClose.Should().Be(st.ConnClosed);
        }

        

    }
}
