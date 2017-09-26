using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    internal class SoakRunWorkItem
    {
        private static readonly string[] queries = new[]
        {
            "RETURN 1295 + 42",
            "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)"
        };

        private static readonly AccessMode[] accessModes = new[]
        {
            AccessMode.Read,
            AccessMode.Write
        };

        private readonly ITestOutputHelper _output;
        private readonly IDriver _driver;
        private readonly IStatisticsCollector _collector;
        private int _counter;

        public SoakRunWorkItem(IDriver driver, IStatisticsCollector collector, ITestOutputHelper output)
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
                var query = queries[currentIteration % 2];
                var accessMode = accessModes[currentIteration % 2];

                using (var session = _driver.Session(accessMode))
                {
                    try
                    {
                        var result = session.Run(query);
                        if (currentIteration % 1000 == 0)
                        {
                            _output.WriteLine(_collector.CollectStatistics().ToContentString());
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
            var query = queries[currentIteration % 2];
            var accessMode = accessModes[currentIteration % 2];

            var session = _driver.Session(accessMode);
            try
            {

                var result = await session.RunAsync(query);
                if (currentIteration % 1000 == 0)
                {
                    _output.WriteLine(_collector.CollectStatistics().ToContentString());
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
