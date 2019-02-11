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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Shared;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class RoutingDriverStressIT : RoutingDriverTestBase
    {
        private Internal.Driver _driver;
        private IMetrics _metrics;
        private ConcurrentQueue<IPooledConnection> _connections;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public RoutingDriverStressIT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
            : base(output, fixture)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            SetupMonitoredDriver();
        }

        [RequireClusterTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void RoutingDriverStressTest(int queryCount)
        {
            const int taskCount = 5;
            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var workItem = new SoakRunWorkItem(_driver, _metrics, Output);

            ConnectionTerminator();
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

        [RequireClusterTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task RoutingDriverStressTestAsync(int queryCount)
        {
            const int taskCount = 5;
            var startTime = DateTime.Now;
            Output.WriteLine($"[{startTime:HH:mm:ss.ffffff}] Started");

            var workItem = new SoakRunWorkItem(_driver, _metrics, Output);

            var terminationTask = ConnectionTerminatorAsync();
            var tasks = new List<Task>();
            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(workItem.RunWithRetriesAsync(queryCount / taskCount));
            }

            await Task.WhenAll(tasks);
            _cancellationTokenSource.Cancel();
            await terminationTask;

            var endTime = DateTime.Now;
            Output.WriteLine($"[{endTime:HH:mm:ss.ffffff}] Finished");
            Output.WriteLine($"Total time spent: {endTime - startTime}");

            PrintStatistics();

            await _driver.CloseAsync();
        }

        private void PrintStatistics()
        {
            var poolMetrics = _metrics.ConnectionPoolMetrics;
            Output.WriteLine(poolMetrics.ToContentString());

            foreach (var value in poolMetrics)
            {
                var st = value.Value;

                st.Creating.Should().Be(0);
                st.Closing.Should().Be(0);
                st.InUse.Should().Be(0);
                st.Idle.Should().Be((int) (st.Created + st.FailedToCreate - st.Closed));
            }
        }

        private void SetupMonitoredDriver()
        {
            var config = new Config
            {
                MetricsFactory = new DefaultMetricsFactory(),
                ConnectionAcquisitionTimeout = TimeSpan.FromMinutes(5),
                ConnectionTimeout = Config.InfiniteInterval,
                MaxConnectionPoolSize = 100,
                DriverLogger = new TestDriverLogger(Output)
            };

            var connectionSettings = new ConnectionSettings(AuthToken, config);
            var bufferSettings = new BufferSettings(config);
            var connectionFactory = new MonitoredPooledConnectionFactory(
                new PooledConnectionFactory(connectionSettings, bufferSettings, config.DriverLogger));

            _driver = (Internal.Driver) GraphDatabase.CreateDriver(new Uri(RoutingServer), config, connectionFactory);
            _connections = connectionFactory.Connections;
            _metrics = _driver.GetMetrics();
        }

        private Task ConnectionTerminator()
        {
            const int minimalConnCount = 3;
            return Task.Run(async () =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (_connections.Count > minimalConnCount && _connections.TryDequeue(out var conn))
                    {
                        await conn.DestroyAsync();
                        Output.WriteLine($"Terminator killed a connection towards server {conn.Server}");
                    }
                    else
                    {
                        Output.WriteLine("Terminator failed to find a open connection to kill.");
                    }

                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }

        private async Task ConnectionTerminatorAsync()
        {
            const int minimalConnCount = 3;
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_connections.Count > minimalConnCount && _connections.TryDequeue(out var conn))
                {
                    await conn.DestroyAsync();
                    Output.WriteLine($"Terminator killed connection {conn} towards server {conn.Server}");
                }
                else
                {
                    Output.WriteLine("Terminator failed to find a open connection to kill.");
                }

                try
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token); // sleep
                }
                catch (TaskCanceledException)
                {
                    // we are fine with cancelled sleep
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _driver?.Close();
        }

        private class MonitoredPooledConnectionFactory : IPooledConnectionFactory
        {
            private readonly IPooledConnectionFactory _delegate;
            public readonly ConcurrentQueue<IPooledConnection> Connections = new ConcurrentQueue<IPooledConnection>();

            public MonitoredPooledConnectionFactory(IPooledConnectionFactory factory)
            {
                _delegate = factory;
            }

            public IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager, IConnectionListener metricsListener)
            {
                var pooledConnection = _delegate.Create(uri, releaseManager, metricsListener);
                Connections.Enqueue(pooledConnection);
                return pooledConnection;
            }
        }
    }
}
