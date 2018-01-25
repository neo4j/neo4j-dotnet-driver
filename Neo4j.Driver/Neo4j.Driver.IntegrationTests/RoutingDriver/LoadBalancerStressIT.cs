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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class LoadBalancerStressIT : RoutingDriverIT
    {
        private Internal.Driver _driver;
        private ConcurrentSet<IPooledConnection> _connections;
        private StatisticsCollector _statisticsCollector;
        private readonly IList<Uri> _clusterMembers;
        private readonly CancellationTokenSource _cancellationTokenSource;


        public LoadBalancerStressIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
            : base(output, fixture)
        {
            _clusterMembers = Cluster.Members.Select(x=>x.BoltRoutingUri).ToList();
            _cancellationTokenSource = new CancellationTokenSource();
            SetupMonitoredDriver();
        }

        [RequireClusterTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500000)]
        public void LoadBalancerStressTests(int queryCount)
        {
            int taskCount = 5;
            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var workItem = new SoakRunWorkItem(_driver, _statisticsCollector, Output);

            ConnectionTerminatorTask();
            var tasks = new List<Task>();
            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(workItem.RunWithRetries(queryCount / taskCount));
            }

            Task.WaitAll(tasks.ToArray());
            _cancellationTokenSource.Cancel();

            _driver.Close();

            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            PrintStatistics();
        }

        private void PrintStatistics()
        {
            var statistics = _statisticsCollector.CollectStatistics();
            Output.WriteLine(statistics.ToContentString());

            foreach (var statistic in statistics)
            {
                var st = ConnectionPoolStatistics.FromDictionary(statistic.Key,
                    statistic.Value.ValueAs<IDictionary<string, object>>());

                st.ConnToCreate.Should().Be(st.ConnCreated + st.ConnFailedToCreate);
                st.ConnToCreate.Should().Be(st.InUseConns + st.AvailableConns + st.ConnToClose);
                st.ConnToClose.Should().Be(st.ConnClosed);
            }
        }

        private void SetupMonitoredDriver()
        {
            _statisticsCollector = new StatisticsCollector();
            var config = new Config
            {
                DriverStatisticsCollector = _statisticsCollector,
                ConnectionAcquisitionTimeout = TimeSpan.FromMinutes(5),
                ConnectionTimeout = Config.InfiniteInterval,
                MaxConnectionPoolSize = 50,
                Logger = new TestLogger(Output)
            };

            var connectionSettings = new ConnectionSettings(AuthToken, config);
            var bufferSettings = new BufferSettings(config);
            var connectionFactory = new MonitoredPoolledConnectionFactory(
                new PooledConnectionFactory(connectionSettings, bufferSettings, config.Logger));

            _driver = (Internal.Driver) GraphDatabase.CreateDriver(new Uri(RoutingServer), config, connectionFactory);
            _connections = connectionFactory.Connections;
        }

        private Task ConnectionTerminatorTask()
        {
            return Task.Run(() =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Output.WriteLine("I am the terminitor.");
                    var conn = _connections.First();
                    conn.Destroy();
                    _connections.TryRemove(conn);
                    Output.WriteLine($"Terminitor closed a connection torwards server {conn.Server}");
                    Task.Delay(10, _cancellationTokenSource.Token).Wait(_cancellationTokenSource.Token); // sleep
                }
            }, _cancellationTokenSource.Token);
        }

        private class MonitoredPoolledConnectionFactory : IPooledConnectionFactory
        {
            private readonly IPooledConnectionFactory _delegate;
            public readonly ConcurrentSet<IPooledConnection> Connections = new ConcurrentSet<IPooledConnection>();

            public MonitoredPoolledConnectionFactory(IPooledConnectionFactory factory)
            {
                _delegate = factory;
            }

            public IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager)
            {
                var pooledConnection = _delegate.Create(uri, releaseManager);
                Connections.TryAdd(pooledConnection);
                return pooledConnection;
            }
        }
    }
}
