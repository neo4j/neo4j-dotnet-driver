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
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class LoadBalancerStressIT : RoutingDriverIT
    {
        private Internal.Driver _driver;
        private IDriverMetrics _metrics;
        private ConcurrentSet<IPooledConnection> _connections;
        private readonly CancellationTokenSource _cancellationTokenSource;


        public LoadBalancerStressIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
            : base(output, fixture)
        {
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

            var workItem = new SoakRunWorkItem(_driver, _metrics, Output);

            ConnectionTerminatorTask();
            var tasks = new List<Task>();
            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(workItem.RunWithRetries(queryCount / taskCount));
            }

            Task.WaitAll(tasks.ToArray());
            _cancellationTokenSource.Cancel();

            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            PrintStatistics();

            _driver.Close();
        }

        private void PrintStatistics()
        {
            var poolMetrics = _metrics.PoolMetrics;
            Output.WriteLine(poolMetrics.ToContentString());

            foreach (var value in poolMetrics)
            {
                var st = value.Value;

                st.ToCreate.Should().Be(0);
                st.ToClose.Should().Be(0);
                st.InUse.Should().Be(0);
                st.Idle.Should().Be((int) (st.Created - st.Closed));
            }
        }

        private void SetupMonitoredDriver()
        {
            var config = new Config
            {
                DriverMetricsEnabled = true,
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
            _metrics = _driver.GetDriverMetrics();
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

            public IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager, IConnectionListener metricsListener)
            {
                var pooledConnection = _delegate.Create(uri, releaseManager, metricsListener);
                Connections.TryAdd(pooledConnection);
                return pooledConnection;
            }
        }
    }
}
