﻿// Copyright (c) "Neo4j"
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

internal class SessionRunWorkloadExecutor(
        IDriver driver,
        IRecordConsumer recordConsumer,
        IWorkloadSessionBuilder sessionBuilder,
        ILogger logger)
    : IWorkloadExecutor
{
    public async Task ExecuteWorkloadAsync(Workload workload)
    {
        await (workload.Mode switch
        {
            Mode.ParallelSessions => ExecuteInParallelSessionsAsync(workload),
            Mode.SequentialSessions => ExecuteSequentialSessionsAsync(workload),
            Mode.SequentialTransactions => ExecuteSequentialTransactionsAsync(workload),
            Mode.SequentialQueries => ExecuteSequentialQueriesAsync(workload),
            _ => throw new ArgumentOutOfRangeException(nameof(workload), "Invalid value for workload mode")
        });
    }

    private async Task ExecuteInParallelSessionsAsync(Workload workload)
    {
        logger.LogDebug("Executing workload in parallel sessions");
        var tasks = new List<Task>();
        foreach (var query in workload.Queries)
        {
            var queryToRun = new Query(query.Text, query.Parameters);
            logger.LogDebug("Starting query {Query} in parallel session", queryToRun.Text);
            tasks.Add(Task.Run(
                async () =>
                {
                    // create a new session in parallel for each query
                    await using var session = sessionBuilder.BuildSession(driver, workload);

                    var resultCursor = await session.RunAsync(queryToRun);
                    var records = await resultCursor.ToListAsync();
                    logger.LogDebug("Received {RecordCount} records", records.Count);
                    recordConsumer.ConsumeRecords(records);
                }));
        }

        logger.LogDebug("Waiting for {TaskCount} parallel tasks to complete", tasks.Count);
        await Task.WhenAll(tasks);

        logger.LogDebug("Workload completed");
    }

    private async Task ExecuteSequentialSessionsAsync(Workload workload)
    {
        logger.LogDebug("Executing workload in sequential sessions");
        foreach (var query in workload.Queries)
        {
            var queryToRun = new Query(query.Text, query.Parameters);

            // create a new session for each query
            logger.LogDebug("Running query {Query} in new session", queryToRun.Text);
            await using var session = sessionBuilder.BuildSession(driver, workload);

            var results = await session.RunAsync(queryToRun);

            var records = await results.ToListAsync();
            logger.LogDebug("Received {RecordCount} records", records.Count);
            recordConsumer.ConsumeRecords(records);
        }

        logger.LogDebug("Workload completed");
    }

    private async Task ExecuteSequentialTransactionsAsync(Workload workload)
    {
        // create one session for the entire workload
        logger.LogDebug("Executing workload in sequential transactions");
        await using var session = sessionBuilder.BuildSession(driver, workload);

        foreach (var query in workload.Queries)
        {
            var queryToRun = new Query(query.Text, query.Parameters);

            logger.LogDebug("Running query {Query} in the same session", queryToRun.Text);
            var results = await session.RunAsync(queryToRun);

            var records = await results.ToListAsync();
            logger.LogDebug("Received {RecordCount} records", records.Count);
            recordConsumer.ConsumeRecords(records);
        }

        logger.LogDebug("Workload completed");
    }

    private async Task ExecuteSequentialQueriesAsync(Workload workload)
    {
        logger.LogDebug("Executing workload in a single transaction");
        await using var session = sessionBuilder.BuildSession(driver, workload);

        // create a single transaction for the entire workload
        await using var transaction = await session.BeginTransactionAsync();

        foreach (var query in workload.Queries)
        {
            var queryToRun = new Query(query.Text, query.Parameters);

            logger.LogDebug("Running query {Query} in the same transaction", queryToRun.Text);
            var results = await transaction.RunAsync(queryToRun);

            var records = await results.ToListAsync();
            logger.LogDebug("Received {RecordCount} records", records.Count);
            recordConsumer.ConsumeRecords(records);
        }

        await transaction.CommitAsync();
        logger.LogDebug("Workload completed");
    }
}
