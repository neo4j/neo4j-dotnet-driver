// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Extensions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;

namespace Neo4j.Driver.IntegrationTests.Stress;

public abstract class StressTest: IDisposable
{
    private enum StressTestMinLogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        None
    }

    private const int DefaultExecutionTime = 30;

    private const int StressTestThreadCount = 8;
    private const int StressTestAsyncBatchSize = 10;

    protected const int BigDataTestBatchCount = 3;
    protected const int BigDataTestBatchSize = 10_000;
    protected const int BigDataTestBatchBuffer = 500;

    private const int PoolTestThreadCount = 50;
    private static TimeSpan StressTestExecutionTime = TimeSpan.FromSeconds(DefaultExecutionTime);
    private static readonly TimeSpan PoolTestDuration = TimeSpan.FromSeconds(15);
    private readonly IAuthToken _authToken;
    private readonly Action<ConfigBuilder> _configure;
    private readonly Uri _databaseUri;
    protected readonly IDriver _driver;

    protected readonly ITestOutputHelper _output;
    private bool _disposed;

    protected StressTest(
        ITestOutputHelper output,
        Uri databaseUri,
        IAuthToken authToken,
        Action<ConfigBuilder> configure = null)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _databaseUri = databaseUri;
        _authToken = authToken;
        _configure = configure;

        var seconds = Environment.GetEnvironmentVariable("TEST_NEO4J_STRESS_DURATION");
        if (!string.IsNullOrEmpty(seconds))
        {
            StressTestExecutionTime =
                TimeSpan.FromSeconds(
                    Convert.ToDouble(seconds) /
                    3); //There are three areas so divide by 3, async, blocking and reactive.
        }

        var minLevel = StressTestMinLogLevel.None;
        var minLevelText = Environment.GetEnvironmentVariable("TEST_NEO4J_STRESS_MIN_LOG_LEVEL");
        if (!string.IsNullOrWhiteSpace(minLevelText))
        {
            Enum.TryParse(minLevelText, out minLevel);
        }

        _driver = GraphDatabase.Driver(
            databaseUri,
            authToken,
            builder =>
            {
                builder
                    .WithLogger(new StressTestLogger(_output, minLevel))
                    .WithMaxConnectionPoolSize(100)
                    .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(1));

                configure?.Invoke(builder);
            });

        CleanupDatabase();
    }

    ~StressTest()
    {
        Dispose(false);
    }

    private class StressTestLogger : ILogger
    {
        private readonly StressTestMinLogLevel _minLevel;
        private readonly ITestOutputHelper _output;

        public StressTestLogger(ITestOutputHelper output, StressTestMinLogLevel minLevel)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _minLevel = minLevel;
        }

        public void Error(Exception cause, string message, params object[] args)
        {
            if (_minLevel <= StressTestMinLogLevel.Error)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    Write(message, args);
                }

                _output.WriteLine(cause.Message);
            }
        }

        public void Warn(Exception cause, string message, params object[] args)
        {
            if (_minLevel <= StressTestMinLogLevel.Warn)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    Write(message, args);
                }

                _output.WriteLine(cause.Message);
            }
        }

        public void Info(string message, params object[] args)
        {
            if (_minLevel <= StressTestMinLogLevel.Info)
            {
                Write(message, args);
            }
        }

        public void Debug(string message, params object[] args)
        {
            if (_minLevel <= StressTestMinLogLevel.Debug)
            {
                Write(message, args);
            }
        }

        public void Trace(string message, params object[] args)
        {
            if (_minLevel <= StressTestMinLogLevel.Trace)
            {
                Write(message, args);
            }
        }

        public bool IsTraceEnabled()
        {
            return StressTestMinLogLevel.Trace == _minLevel;
        }

        public bool IsDebugEnabled()
        {
            return _minLevel <= StressTestMinLogLevel.Debug;
        }

        private void Write(string message, params object[] args)
        {
            _output.WriteLine(message, args);
        }
    }

#region Abstract Members

    protected abstract StressTestContext CreateContext();

    protected abstract IEnumerable<IBlockingCommand> CreateTestSpecificBlockingCommands();

    protected abstract IEnumerable<IAsyncCommand> CreateTestSpecificAsyncCommands();

    protected abstract IEnumerable<IRxCommand> CreateTestSpecificRxCommands();

    protected abstract void PrintStats(StressTestContext context);

    protected abstract void VerifyReadQueryDistribution(StressTestContext context);

    protected virtual void RunReactiveBigData()
    {
        //Base implementation does nothing.
    }

    public abstract bool HandleWriteFailure(Exception error, StressTestContext context);

#endregion

#region Blocking Stress Test

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task Blocking()
    {
        await RunStressTest(LaunchBlockingWorkers);
    }

    private IList<IBlockingCommand> CreateBlockingCommands()
    {
        var result = new List<IBlockingCommand>
        {
            new BlockingReadCommandTxFunc(_driver, false),
            new BlockingReadCommandTxFunc(_driver, true),
            new BlockingWriteCommandTxFunc(this, _driver, false),
            new BlockingWriteCommandTxFunc(this, _driver, true),
            new BlockingWrongCommandTxFunc(_driver),
            new BlockingFailingCommandTxFunc(_driver)
        };

        result.AddRange(CreateTestSpecificBlockingCommands());

        return result;
    }

    private IEnumerable<Task> LaunchBlockingWorkers(StressTestContext context)
    {
        var commands = CreateBlockingCommands();

        var tasks = new List<Task>();
        for (var i = 0; i < StressTestThreadCount; i++)
        {
            tasks.Add(LaunchBlockingWorkerThread(context, commands));
        }

        return tasks;
    }

    private static Task LaunchBlockingWorkerThread(StressTestContext context, IList<IBlockingCommand> commands)
    {
        return Task.Factory.StartNew(
            () =>
            {
                while (!context.Stopped)
                {
                    commands.RandomElement().Execute(context);
                }
            },
            TaskCreationOptions.LongRunning);
    }

#endregion

#region Async Stress Test

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task Async()
    {
        await RunStressTest(LaunchAsyncWorkers);
    }

    private IList<IAsyncCommand> CreateAsyncCommands()
    {
        /* 
            Optional tests that can be run. Currenlty only want to run the transaction functions as these are what are used with Aura
            AsyncReadCommand
            AsyncReadCommandInTx
            AsyncReadCommandTxFunc
            AsyncWriteCommand
            AsyncWriteCommandInTx
            AsyncWriteCommandTxFunc
            AsyncWrongCommand
            AsyncWrongCommandInTx
            AsyncWrongCommandTxFunc
            AsyncFailingCommand
            AsyncFailingCommandInTx
            AsyncFailingCommandTxFunc
        */

        var result = new List<IAsyncCommand>
        {
            new AsyncReadCommandTxFunc(_driver, false),
            new AsyncReadCommandTxFunc(_driver, true),
            new AsyncWriteCommandTxFunc(this, _driver, false),
            new AsyncWriteCommandTxFunc(this, _driver, true),
            new AsyncWrongCommandTxFunc(_driver),
            new AsyncFailingCommandTxFunc(_driver)
        };

        result.AddRange(CreateTestSpecificAsyncCommands());

        return result;
    }

    private IEnumerable<Task> LaunchAsyncWorkers(StressTestContext context)
    {
        var commands = CreateAsyncCommands();

        var tasks = new List<Task>();
        for (var i = 0; i < StressTestThreadCount; i++)
        {
            tasks.Add(LaunchAsyncWorkerThread(context, commands));
        }

        return tasks;
    }

    private static Task LaunchAsyncWorkerThread(StressTestContext context, IList<IAsyncCommand> commands)
    {
        return Task.Factory.StartNew(
            () =>
            {
                while (!context.Stopped)
                {
                    Task.WaitAll(
                        Enumerable.Range(1, StressTestAsyncBatchSize)
                            .Select(_ => commands.RandomElement().ExecuteAsync(context))
                            .ToArray());
                }
            },
            TaskCreationOptions.LongRunning);
    }

#endregion

#region Reactive Stress Test

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task Reactive()
    {
        await RunStressTest(LaunchRxWorkers);
    }

    private IList<IRxCommand> CreateRxCommands()
    {
        var result = new List<IRxCommand>();
        result.AddRange(CreateTestSpecificRxCommands());
        return result;
    }

    private IEnumerable<Task> LaunchRxWorkers(StressTestContext context)
    {
        var commands = CreateRxCommands();
        var tasks = new List<Task>();

        if (commands.Count > 0)
        {
            for (var i = 0; i < StressTestThreadCount; i++)
            {
                tasks.Add(LaunchRxWorkerThread(context, commands));
            }
        }

        return tasks;
    }

    private static Task LaunchRxWorkerThread(StressTestContext context, IList<IRxCommand> commands)
    {
        return Task.Factory.StartNew(
            () =>
            {
                while (!context.Stopped)
                {
                    Task.WaitAll(
                        Enumerable.Range(1, StressTestAsyncBatchSize)
                            .Select(_ => commands.RandomElement().ExecuteAsync(context))
                            .ToArray());
                }
            },
            TaskCreationOptions.LongRunning);
    }

#endregion

#region Async Big Data Tests

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task AsyncBigData()
    {
        var bookmark =
            await CreateNodesAsync(BigDataTestBatchCount, BigDataTestBatchSize, BigDataTestBatchBuffer, _driver);

        await ReadNodesAsync(_driver, bookmark, BigDataTestBatchCount * BigDataTestBatchSize);
    }

    private async Task<Bookmarks> CreateNodesAsync(int batchCount, int batchSize, int queryBatchSize, IDriver driver)
    {
        var timer = Stopwatch.StartNew();

        var session = driver.AsyncSession();
        try
        {
            for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        // 1-500, 501-1000
                        var batches = Enumerable.Range(batchIndex * batchSize + 1, batchSize).Batch(queryBatchSize);    

                        foreach (var batch in batches)
                        {
                            var cursor = await tx.RunAsync(CreateBatchNodesQuery(batch));
                            await cursor.ConsumeAsync();
                        }
                    });
            }
        }
        finally
        {
            await session.CloseAsync();
        }

        _output.WriteLine("Creating nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);

        return session.LastBookmarks;
    }

    private Query CreateBatchNodesQuery(IEnumerable<int> batch)
    {
        return new Query(
            "UNWIND $values AS props CREATE (n:Test:Node) SET n = props",
            new
            {
                values = batch.Select(
                    nodeIndex => new
                    {
                        index = nodeIndex,
                        name = $"name-{nodeIndex}",
                        surname = $"surname-{nodeIndex}",
                        longList = Enumerable.Repeat((long)nodeIndex, 10),
                        doubleList = Enumerable.Repeat((double)nodeIndex, 10),
                        boolList = Enumerable.Repeat(nodeIndex % 2 == 0, 10)
                    })
            });
    }

    private async Task ReadNodesAsync(IDriver driver, Bookmarks bookmarks, int expectedNodes)
    {
        var timer = Stopwatch.StartNew();

        var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(bookmarks));
        try
        {
            await session.ExecuteReadAsync(
                async txc =>
                {
                    var records = await txc.RunAsync("MATCH (n:Node) RETURN n ORDER BY n.index")
                        .ContinueWith(r => r.Result.ToListAsync())
                        .Unwrap();

                    records.Select(r => r[0].As<INode>())
                        .Should()
                        .BeEquivalentTo(
                            Enumerable.Range(1, expectedNodes)
                                .Select(
                                    index =>
                                        new
                                        {
                                            Labels = new[] { "Test", "Node" },
                                            Properties = new Dictionary<string, object>(6)
                                            {
                                                { "index", (long)index },
                                                { "name", $"name-{index}" },
                                                { "surname", $"surname-{index}" },
                                                {
                                                    "longList",
                                                    Enumerable.Repeat((long)index, 10)
                                                },
                                                {
                                                    "doubleList",
                                                    Enumerable.Repeat((double)index, 10)
                                                },
                                                {
                                                    "boolList",
                                                    Enumerable.Repeat(index % 2 == 0, 10)
                                                }
                                            }
                                        }),
                            opts => opts.Including(x => x.Labels).Including(x => x.Properties));
                });
        }
        finally
        {
            await session.CloseAsync();
        }

        _output.WriteLine("Reading nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);
    }

#endregion

#region Blocking Big Data Tests

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void BlockingBigData()
    {
        var bookmark = CreateNodes(BigDataTestBatchCount, BigDataTestBatchSize, BigDataTestBatchBuffer, _driver);
        ReadNodes(_driver, bookmark, BigDataTestBatchCount * BigDataTestBatchSize);
    }

    private Bookmarks CreateNodes(int batchCount, int batchSize, int batchBuffer, IDriver driver)
    {
        var timer = Stopwatch.StartNew();

        using (var session = driver.Session())
        {
            for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                var index = batchIndex;

                session.ExecuteWrite(
                    txc =>
                        Enumerable.Range(1, batchSize)
                            .Select(item => index * batchSize + item)
                            .Batch(batchBuffer)
                            .Select(indices => txc.Run(CreateBatchNodesQuery(indices)).Consume())
                            .ToArray());
            }

            _output.WriteLine("Creating nodes with Sync API took: {0}ms", timer.ElapsedMilliseconds);

            return session.LastBookmarks;
        }
    }

    private void ReadNodes(IDriver driver, Bookmarks bookmarks, int expectedNodes)
    {
        var timer = Stopwatch.StartNew();

        using (var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(bookmarks)))
        {
            session.ExecuteRead(
                txc =>
                {
                    var result = txc.Run("MATCH (n:Node) RETURN n ORDER BY n.index");

                    result.Select(r => r[0].As<INode>())
                        .Should()
                        .BeEquivalentTo(
                            Enumerable.Range(1, expectedNodes)
                                .Select(
                                    index =>
                                        new
                                        {
                                            Labels = new[] { "Test", "Node" },
                                            Properties = new Dictionary<string, object>(6)
                                            {
                                                { "index", (long)index },
                                                { "name", $"name-{index}" },
                                                { "surname", $"surname-{index}" },
                                                { "longList", Enumerable.Repeat((long)index, 10) },
                                                { "doubleList", Enumerable.Repeat((double)index, 10) },
                                                { "boolList", Enumerable.Repeat(index % 2 == 0, 10) }
                                            }
                                        }),
                            opts => opts.Including(x => x.Labels).Including(x => x.Properties));

                    return result.Consume();
                });
        }

        _output.WriteLine("Reading nodes with Sync API took: {0}ms", timer.ElapsedMilliseconds);
    }

#endregion

#region Reactive Big Data Tests

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void ReactiveBigData()
    {
        RunReactiveBigData();
    }

    protected Bookmarks CreateNodesRx(int batchCount, int batchSize, int batchBuffer, IDriver driver)
    {
        var timer = Stopwatch.StartNew();
        var session = driver.RxSession();

        Observable.Range(0, batchCount)
            .Select(
                batchIndex =>
                    session.ExecuteWrite(
                        txc => Observable.Range(1, batchSize)
                            .Select(index => batchIndex * batchSize + index)
                            .Buffer(batchBuffer)
                            .SelectMany(batch => txc.Run(CreateBatchNodesQuery(batch)).Consume())))
            .Concat()
            .Concat(session.Close<IResultSummary>())
            .CatchAndThrow(_ => session.Close<IResultSummary>())
            .Wait();

        _output.WriteLine("Creating nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);

        return session.LastBookmarks;
    }

    protected void ReadNodesRx(IDriver driver, Bookmarks bookmarks, int expectedNodes)
    {
        var timer = Stopwatch.StartNew();

        var session = driver.RxSession(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(bookmarks));

        session.ExecuteRead(
                txc =>
                    txc.Run("MATCH (n:Node) RETURN n ORDER BY n.index")
                        .Records()
                        .Select(r => r[0].As<INode>())
                        .Do(
                            n =>
                            {
                                var index = n.Properties["index"].As<int>();

                                n.Should()
                                    .BeEquivalentTo(
                                        new
                                        {
                                            Labels = new[] { "Test", "Node" },
                                            Properties = new Dictionary<string, object>(6)
                                            {
                                                { "index", (long)index },
                                                { "name", $"name-{index}" },
                                                { "surname", $"surname-{index}" },
                                                {
                                                    "longList",
                                                    Enumerable.Repeat((long)index, 10)
                                                },
                                                {
                                                    "doubleList",
                                                    Enumerable.Repeat((double)index, 10)
                                                },
                                                {
                                                    "boolList",
                                                    Enumerable.Repeat(index % 2 == 0, 10)
                                                }
                                            }
                                        },
                                        opts => opts.Including(x => x.Labels).Including(x => x.Properties));
                            }))
            .Concat(session.Close<INode>())
            .CatchAndThrow(_ => session.Close<INode>())
            .Wait();

        _output.WriteLine("Reading nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);
    }

#endregion

#region Pooling Stress Tests

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void Pool()
    {
        var tokenSource = new CancellationTokenSource();
        var failure = new AtomicReference<Exception>(null);

        var tasks = LaunchPoolWorkers(_driver, tokenSource.Token, worker => worker.RunWithNoTx(), failure);
        Thread.Sleep(PoolTestDuration);
        tokenSource.Cancel();
        Task.WaitAll(tasks);

        failure.Get().Should().BeNull("Some workers have failed. Exception {failure.Get()?.Message}");
    }

    [RequireServerFact]
    public void PoolWithTxFunc()
    {
        var tokenSource = new CancellationTokenSource();
        var failure = new AtomicReference<Exception>(null);

        var tasks = LaunchPoolWorkers(_driver, tokenSource.Token, worker => worker.RunWithTxFunc(), failure);
        Thread.Sleep(PoolTestDuration);
        tokenSource.Cancel();
        Task.WaitAll(tasks);

        failure.Get().Should().BeNull($"Some workers have failed. Exception {failure.Get()?.Message}");
    }

    private static Task[] LaunchPoolWorkers(
        IDriver driver,
        CancellationToken token,
        Action<Worker> job,
        AtomicReference<Exception> failure)
    {
        var tasks = new List<Task>();
        for (var i = 0; i < PoolTestThreadCount; i++)
        {
            tasks.Add(
                Task.Factory.StartNew(
                    () =>
                    {
                        var worker = new Worker(driver, token, failure);
                        job(worker);
                    },
                    TaskCreationOptions.LongRunning));
        }

        return tasks.ToArray();
    }

    private class Worker
    {
        private static readonly (AccessMode, string)[] Queries =
        {
            (AccessMode.Read, "RETURN 1295+42"),
            (AccessMode.Write, "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n")
        };

        private readonly IDriver _driver;
        private readonly AtomicReference<Exception> _failure;
        private readonly Random _rnd = new();
        private readonly CancellationToken _token;

        public Worker(IDriver driver, CancellationToken token, AtomicReference<Exception> failure)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _token = token;
            _failure = failure;
        }

        public void RunWithNoTx()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    foreach (var (accessMode, query) in Queries)
                    {
                        RunWithNoTx(accessMode, query);
                    }
                }
            }
            catch (Exception exc)
            {
                _failure.CompareAndSet(exc, null);
            }
        }

        public void RunWithTxFunc()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    foreach (var (accessMode, query) in Queries)
                    {
                        RunWithTxFunc(accessMode, query);
                    }
                }
            }
            catch (Exception exc)
            {
                _failure.CompareAndSet(exc, null);
            }
        }

        private void RunWithNoTx(AccessMode mode, string query)
        {
            using (var session = _driver.Session(o => o.WithDefaultAccessMode(mode)))
            {
                Execute(session, query);
            }
        }

        private void RunWithTxFunc(AccessMode mode, string query)
        {
            using (var session = _driver.Session())
            {
                switch (mode)
                {
                    case AccessMode.Read:
                        session.ExecuteRead(txc => Execute(txc, query));
                        break;

                    case AccessMode.Write:
                        session.ExecuteWrite(txc => Execute(txc, query));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }
        }

        private IResultSummary Execute(IQueryRunner runner, string query)
        {
            var result = runner.Run(query);
            Thread.Sleep(_rnd.Next(100));
            result.Consume();
            Thread.Sleep(_rnd.Next(100));
            return result.Consume();
        }
    }

    private class AtomicReference<T>
        where T : class
    {
        private volatile T _reference;

        public AtomicReference(T reference)
        {
            _reference = reference;
        }

        public T Get()
        {
            return _reference;
        }

        public bool CompareAndSet(T value, T comparand)
        {
            return Interlocked.CompareExchange(ref _reference, value, comparand) == comparand;
        }
    }

    private class MonitoredPooledConnectionFactory : IPooledConnectionFactory
    {
        private readonly IPooledConnectionFactory _delegate;
        public readonly ConcurrentQueue<IPooledConnection> Connections = new();

        public MonitoredPooledConnectionFactory(IPooledConnectionFactory factory)
        {
            _delegate = factory;
        }

        public IPooledConnection Create(
            Uri uri,
            IConnectionReleaseManager releaseManager,
            IDictionary<string, string> routingContext)
        {
            var pooledConnection = _delegate.Create(uri, releaseManager, routingContext);
            Connections.Enqueue(pooledConnection);
            return pooledConnection;
        }
    }

#endregion

#region Test and Verifications

    private async Task RunStressTest(Func<StressTestContext, IEnumerable<Task>> launcher)
    {
        var context = CreateContext();
        var workers = launcher(context);

        if (!workers.Any())
        {
            return;
        }

        await Task.Delay(StressTestExecutionTime); //Divide by three because there are three sets of tests
        context.Stop();

        await Task.WhenAll(workers);

        PrintStats(context);

        VerifyResults(context);
    }

    private void VerifyResults(StressTestContext context)
    {
        VerifyNodesCreated(context.CreatedNodesCount);
        VerifyReadQueryDistribution(context);
    }

    private void VerifyNodesCreated(long expected)
    {
        using (var session = _driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
        {
            var count = session.Run("MATCH (n) RETURN count(n) as createdNodes")
                .Select(r => r["createdNodes"].As<long>())
                .Single();

            count.Should().Be(expected);
        }
    }

#endregion

#region Cleanup

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CleanupDatabase();
        }

        //Mark as disposed
        _disposed = true;
    }

    private void CleanupDatabase()
    {
        using (var session = _driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
        {
            session.Run("MATCH (n) DETACH DELETE n").Consume();
        }
    }

#endregion
}
