// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Neo4j.Driver.Tests.BenchkitBackend.Abstractions;
using Neo4j.Driver.Tests.BenchkitBackend.Types;

namespace Neo4j.Driver.Tests.BenchkitBackend.Implementations;

using ILogger = Microsoft.Extensions.Logging.ILogger;

internal class ExecuteQueryWorkloadExecutor(
        IDriver driver,
        IRecordConsumer recordConsumer,
        ILogger logger)
    : IWorkloadExecutor
{
    public async Task ExecuteWorkloadAsync(Workload workload)
    {
        if (workload.Mode == Mode.ParallelSessions)
        {
            await ExecuteInParallel(workload);
        }
        else
        {
            await ExecuteInSeries(workload);
        }
    }

    private async Task ExecuteInSeries(Workload workload)
    {
        logger.LogDebug("Executing workload in series");
        foreach (var query in workload.Queries)
        {
            await ExecuteQueryRunAndConsume(query, workload)
                .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        logger.LogDebug("Workload completed");
    }

    private async Task ExecuteInParallel(Workload workload)
    {
        logger.LogDebug("Executing workload in parallel");
        var tasks = workload.Queries.Select(query => ExecuteQueryRunAndConsume(query, workload)).ToList();
        logger.LogDebug("Waiting for {N} parallel tasks to complete", tasks.Count);
        await Task.WhenAll(tasks);
        logger.LogDebug("Workload completed");
    }

    private async Task ExecuteQueryRunAndConsume(WorkloadQuery workloadQuery, Workload workload)
    {
        var config = new QueryConfig(workload.Routing.ToRoutingControl(), workload.Database);

        logger.LogDebug("Starting query {Query}", workloadQuery.Text);
        var (results, _) = await driver
            .ExecutableQuery(workloadQuery.Text)
            .WithParameters(workloadQuery.Parameters)
            .WithConfig(config)
            .ExecuteAsync();

        logger.LogDebug("Consuming {N} results", results.Count);
        recordConsumer.ConsumeRecords(results);
    }
}
