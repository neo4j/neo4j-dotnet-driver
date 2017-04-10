// Copyright (c) 2002-2017 "Neo Technology,"
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
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

        [Require31ServerFact]
        public void ShouldConnectIPv6Address()
        {
            using (var driver = GraphDatabase.Driver("bolt://[::1]:7687", AuthToken))
            using (var session = driver.Session())
            {
                var ret = session.Run("RETURN 1").Single();
                ret[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerFact]
        public void ShouldConnectIPv4Address()
        {
            using (var driver = GraphDatabase.Driver("bolt://127.0.0.1:7687", AuthToken))
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
                var st = ConnectionPoolStatistics.Read(statisticsCollector.CollectStatistics());
                Output.WriteLine(st.ReportStatistics().ToContentString());
                st.ConnCreated.Should().Be(sessionCount);
                st.ConnCreated.Should().Be(st.ConnClosed + 1);
            }
        }

        [RequireServerTheory]
        [InlineData(5000)]
//        [InlineData(50000)] leave this to a long dedicated build
        public void SoakRun(int threadCount)
        {
            var statisticsCollector = new StatisticsCollector();
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken, new Config
                {
                    DriverStatisticsCollector = statisticsCollector,
                    ConnectionTimeout = Config.Infinite,
                    EncryptionLevel = EncryptionLevel.Encrypted
                });

            Output.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Started");

            Parallel.For(0, threadCount, i =>
            {
                if (i % 1000 == 0)
                {
                    Output.WriteLine(statisticsCollector.CollectStatistics().ToContentString());
                }

                string[] queries = { "RETURN 1295 + 42", "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)" };
                try
                {
                    using (var session = driver.Session())
                    {
                        session.Run(queries[i % 2]).Consume();
                    }
                }
                catch (Exception e)
                {
                    Output.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Thread {i} failed to run query {queries[i%2]} due to {e.Message}");
                }
            });

            var st = ConnectionPoolStatistics.Read(statisticsCollector.CollectStatistics());
            Output.WriteLine(st.ReportStatistics().ToContentString());
            Output.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Finished");

            st.ConnToCreate.Should().Be(st.ConnCreated + st.ConnFailedToCreate);
            st.ConnToCreate.Should().Be(st.InUseConns + st.AvailableConns + st.ConnToClose);
            st.ConnClosed.Should().Be(st.ConnClosed);
            st.ConnToCreate.Should().Be(st.ConnCreated);

            driver.Dispose();
        }
    }
}