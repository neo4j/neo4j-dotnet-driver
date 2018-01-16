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

        public class DriverMetrics : IDriverMetrics
        {
            public IDictionary<string, IConnectionPoolMetrics> PoolMetrics { get; }
            public IDictionary<string, IConnectionMetrics> ConnectionMetrics { get; }

            public DriverMetrics()
            {
                PoolMetrics = new Dictionary<string, IConnectionPoolMetrics>();
                ConnectionMetrics = new Dictionary<string, IConnectionMetrics>();
            }
        }

        private readonly ITestOutputHelper _output;
        private readonly IDriver _driver;
        private readonly IDriverMetrics _collector;
        private int _counter;

        public SoakRunWorkItem(IDriver driver, IDriverMetrics collector, ITestOutputHelper output)
        {
            this._driver = driver;
            this._collector = collector;
            this._output = output;
        }

        public Task Run()
        {
            return Task.Run(() =>
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
                            _output.WriteLine(_collector.PoolMetrics.ToContentString());
                        }

                        result.Consume();
                    }
                    catch (Exception e)
                    {
                        _output.WriteLine(
                            $"[{DateTime.Now:HH:mm:ss.ffffff}] Iteration {currentIteration} failed to run query {query} due to {e.Message}");
                    }
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
                    _output.WriteLine(_collector.PoolMetrics.ToContentString());
                }
                await result.SummaryAsync();
            }
            catch (Exception e)
            {
                _output.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.ffffff}] Iteration {currentIteration} failed to run query {query} due to {e.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

    }

}
