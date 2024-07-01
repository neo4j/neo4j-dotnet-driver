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

internal class ExecuteReadWriteWorkloadExecutor(
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
            tasks.Add(
                Task.Run(
                    async () =>
                    {
                        // create a new session in parallel for each query
                        await using var session = sessionBuilder.BuildSession(driver, workload);

                        var records = await ExecuteReadOrWriteAsync(queryToRun, workload.Method, session);
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
            logger.LogDebug("Starting query {Query} in new session", queryToRun.Text);

            // create a new session for each query
            await using var session = sessionBuilder.BuildSession(driver, workload);

            var records = await ExecuteReadOrWriteAsync(queryToRun, workload.Method, session);
            logger.LogDebug("Received {RecordCount} records", records.Count);
            recordConsumer.ConsumeRecords(records);
        }

        logger.LogDebug("Workload completed");
    }

    private async Task ExecuteSequentialTransactionsAsync(Workload workload)
    {
        logger.LogDebug("Executing workload in sequential transactions");

        // create one session to use for all queries
        await using var session = sessionBuilder.BuildSession(driver, workload);

        foreach (var query in workload.Queries)
        {
            var queryToRun = new Query(query.Text, query.Parameters);
            logger.LogDebug("Starting query {Query} in new transaction", queryToRun.Text);

            var records = await ExecuteReadOrWriteAsync(queryToRun, workload.Method, session);
            logger.LogDebug("Received {RecordCount} records", records.Count);
            recordConsumer.ConsumeRecords(records);
        }

        logger.LogDebug("Workload completed");
    }

    private static Task<List<IRecord>> ExecuteReadOrWriteAsync(Query query, Method method, IAsyncSession session)
    {
        return method switch
        {
            Method.ExecuteRead => session.ExecuteReadAsync(t => RunQuery(query, t)),
            Method.ExecuteWrite => session.ExecuteWriteAsync(t => RunQuery(query, t)),
            _ => throw new ArgumentOutOfRangeException(nameof(method), "Invalid value for method")
        };
    }

    private async Task ExecuteSequentialQueriesAsync(Workload workload)
    {
        logger.LogDebug("Executing workload in sequential queries");

        // create one session to use for all queries
        await using var session = sessionBuilder.BuildSession(driver, workload);

        // decide which session method we're going to call
        Func<Func<IAsyncQueryRunner, Task<int>>, Action<TransactionConfigBuilder>, Task<int>>
            execute = workload.Method switch
            {
                Method.ExecuteRead => session.ExecuteReadAsync,
                Method.ExecuteWrite => session.ExecuteWriteAsync,
                _ => throw new ArgumentOutOfRangeException(nameof(workload), "Invalid value for workload method")
            };

        await execute(
            async tx =>
            {
                // loop through each query in the workload in the same transaction
                foreach (var query in workload.Queries)
                {
                    var queryToRun = new Query(query.Text, query.Parameters);
                    logger.LogDebug("Starting query {Query} in same transaction", queryToRun.Text);
                    var records = await RunQuery(queryToRun, tx);
                    logger.LogDebug("Received {RecordCount} records", records.Count);
                    recordConsumer.ConsumeRecords(records);
                }

                return 0;
            },
            _ => { });

        logger.LogDebug("Workload completed");
    }

    private static async Task<List<IRecord>> RunQuery(Query query, IAsyncQueryRunner transaction)
    {
        var cursor = await transaction.RunAsync(query.Text, query.Parameters);
        var records = await cursor.ToListAsync();
        return records;
    }
}
