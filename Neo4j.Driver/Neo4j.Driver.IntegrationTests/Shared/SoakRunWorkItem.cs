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
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    internal class SoakRunWorkItem
    {
        private static readonly string[] queries = new[]
        {
            "RETURN 1295 + 42",
            "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)",
        };

        private static readonly AccessMode[] accessModes = new[]
        {
            AccessMode.Read,
            AccessMode.Write
        };

        private readonly ITestOutputHelper _output;
        private readonly IDriver _driver;
        private readonly IDriverMetrics _driverMetrics;
        private int _counter;

        public SoakRunWorkItem(IDriver driver, IDriverMetrics driverMetrics, ITestOutputHelper output)
        {
            this._driver = driver;
            this._driverMetrics = driverMetrics;
            this._output = output;
        }

        public Task Run(int times = 1)
        {
            return Task.Run(() =>
            {
                for (var i = 0; i < times; i++)
                {
                    var currentIteration = Interlocked.Increment(ref _counter);
                    var query = queries[currentIteration % queries.Length];
                    var accessMode = accessModes[currentIteration % accessModes.Length];

                    using (var session = _driver.Session(accessMode))
                    {
                        try
                        {
                            var result = session.Run(query);
                            if (currentIteration % 1000 == 0)
                            {
                                _output.WriteLine(_driverMetrics.PoolMetrics.ToContentString());
                            }

                            result.Consume();
                        }
                        catch (Exception e)
                        {
                            _output.WriteLine(
                                $"[{DateTime.Now:HH:mm:ss.ffffff}] " +
                                $"Iteration {currentIteration} failed to run query {query} due to {e.Message}");
                            _output.WriteLine(e.StackTrace);
                        }
                    }
                    // pause for each duration
                    Task.Delay(10).Wait();
                }
            });
        }

        public async Task RunAsync()
        {
            var currentIteration = Interlocked.Increment(ref _counter);
            var query = queries[currentIteration % queries.Length];
            var accessMode = accessModes[currentIteration % accessModes.Length];

            var session = _driver.Session(accessMode);
            try
            {

                var result = await session.RunAsync(query);
                if (currentIteration % 1000 == 0)
                {
                    _output.WriteLine(_driverMetrics.PoolMetrics.ToContentString());
                }
                await result.SummaryAsync();
            }
            catch (Exception e)
            {
                _output.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.ffffff}] Iteration {currentIteration} failed to run query {query} due to {e.Message}");
                _output.WriteLine(e.StackTrace);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public Task RunWithRetries(int repeat = 1)
        {
            return Task.Run(() =>
            {
                for (var i = 0; i < repeat; i++)
                {
                    var currentIteration = Interlocked.Increment(ref _counter);
                    var query = queries[currentIteration % queries.Length];
                    var accessMode = accessModes[currentIteration % accessModes.Length];

                    using (var session = _driver.Session())
                    {
                        try
                        {
                            void Action(ITransaction tx)
                            {
                                tx.Run(query).Consume();
                            }

                            if (accessMode == AccessMode.Read)
                            {
                                session.ReadTransaction(Action);
                            }
                            else
                            {
                                session.WriteTransaction(Action);
                            }

                            if (currentIteration % 1000 == 0)
                            {
                                _output.WriteLine(_collector.CollectStatistics().ToContentString());
                            }
                        }
                        catch (Exception e)
                        {
                            _output.WriteLine(
                                $"[{DateTime.Now:HH:mm:ss.ffffff}] Iteration {currentIteration} failed to run query {query} due to {e.Message}");
                        }
                    }

                    Task.Delay(10).Wait();
                }
            });
        }

    }

}
