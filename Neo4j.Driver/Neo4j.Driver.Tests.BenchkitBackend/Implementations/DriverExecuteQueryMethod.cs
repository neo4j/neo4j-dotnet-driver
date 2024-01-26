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

namespace Neo4j.Driver.Tests.BenchkitBackend.Implementations;

using ILogger = Microsoft.Extensions.Logging.ILogger;

internal class DriverExecuteQueryMethod(
        IDriver driver,
        IRecordConsumer recordConsumer,
        ILogger logger)
    : IWorkloadExecutionMethod
{
    /// <inheritdoc />
    public async Task ExecuteWorkloadAsync(Workload workload)
    {
        var tasks = new List<Task>();
        foreach (var query in workload.Queries)
        {
            if (workload.Mode == Mode.ParallelSessions)
            {
                tasks.Add(ExecuteQueryRunAndConsume(query));
            }
            else
            {
                await ExecuteQueryRunAndConsume(query);
                logger.LogDebug("Query completed");
            }
        }

        if (!tasks.Any())
        {
            return;
        }

        logger.LogDebug("Waiting for {N} parallel tasks to complete", tasks.Count);
        await Task.WhenAll(tasks);
        logger.LogDebug("All parallel tasks completed");
    }

    private async Task ExecuteQueryRunAndConsume(Query query)
    {
        logger.LogDebug("Starting query {Query}", query.Text);
        var (results, _) = await driver
            .ExecutableQuery(query.Text)
            .WithParameters(query.Parameters)
            .ExecuteAsync();
        logger.LogDebug("Consuming {N} results", results.Count);
        recordConsumer.ConsumeRecords(results);
    }
}
