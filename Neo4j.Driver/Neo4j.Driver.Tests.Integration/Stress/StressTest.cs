// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Reactive;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.VersionComparison;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public abstract class StressTest<TContext> : IDisposable
        where TContext : StressTestContext
    {
        private bool _disposed = false;
        private const bool LoggingEnabled = false;

        private const int StressTestThreadCount = 8;
        private const int StressTestAsyncBatchSize = 10;
        private static readonly TimeSpan StressTestExecutionTime = TimeSpan.FromSeconds(30);

        private const int BigDataTestBatchCount = 3;
        private const int BigDataTestBatchSize = 10_000;
        private const int BigDataTestBatchBuffer = 500;

        private const int PoolTestThreadCount = 50;
        private static readonly TimeSpan PoolTestDuration = TimeSpan.FromSeconds(15);

        protected readonly ITestOutputHelper _output;
        protected readonly IDriver _driver;
        private readonly IAuthToken _authToken;
        private readonly Uri _databaseUri;
        private readonly Action<ConfigBuilder> _configure;

        ~StressTest() => Dispose(false);

        protected StressTest(ITestOutputHelper output, Uri databaseUri, IAuthToken authToken, Action<ConfigBuilder> configure = null)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _databaseUri = databaseUri;
            _authToken = authToken;
            _configure = configure;

            _driver = GraphDatabase.Driver(databaseUri, authToken, builder =>
            {
                builder
                    .WithLogger(new StressTestLogger(_output, LoggingEnabled))
                    .WithMaxConnectionPoolSize(100)
                    .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(1));
                configure?.Invoke(builder);
            });

            CleanupDatabase();
        }

        #region Abstract Members

        protected abstract TContext CreateContext();

        protected abstract IEnumerable<IBlockingCommand<TContext>> CreateTestSpecificBlockingCommands();

        protected abstract IEnumerable<IAsyncCommand<TContext>> CreateTestSpecificAsyncCommands();

        protected abstract IEnumerable<IRxCommand<TContext>> CreateTestSpecificRxCommands();

        protected abstract void PrintStats(TContext context);

        protected abstract void VerifyReadQueryDistribution(TContext context);

        public abstract bool HandleWriteFailure(Exception error, TContext context);

		#endregion

		#region Blocking Stress Test

		[RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
		public async Task Blocking()
        {
            await RunStressTest(LaunchBlockingWorkers);
        }

        private IList<IBlockingCommand<TContext>> CreateBlockingCommands()
        {
            var result = new List<IBlockingCommand<TContext>>
            {
                new BlockingReadCommand<TContext>(_driver, false),
                new BlockingReadCommand<TContext>(_driver, true),
                new BlockingReadCommandInTx<TContext>(_driver, false),
                new BlockingReadCommandInTx<TContext>(_driver, true),
                new BlockingWriteCommand<TContext>(this, _driver, false),
                new BlockingWriteCommand<TContext>(this, _driver, true),
                new BlockingWriteCommandInTx<TContext>(this, _driver, false),
                new BlockingWriteCommandInTx<TContext>(this, _driver, true),
                new BlockingWrongCommand<TContext>(_driver),
                new BlockingWrongCommandInTx<TContext>(_driver),
                new BlockingFailingCommand<TContext>(_driver),
                new BlockingFailingCommandInTx<TContext>(_driver)
            };

            result.AddRange(CreateTestSpecificBlockingCommands());

            return result;
        }

        private IEnumerable<Task> LaunchBlockingWorkers(TContext context)
        {
            var commands = CreateBlockingCommands();

            var tasks = new List<Task>();
            for (var i = 0; i < StressTestThreadCount; i++)
            {
                tasks.Add(LaunchBlockingWorkerThread(context, commands));
            }

            return tasks;
        }

        private static Task LaunchBlockingWorkerThread(TContext context, IList<IBlockingCommand<TContext>> commands)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!context.Stopped)
                {
                    commands.RandomElement().Execute(context);
                }
            }, TaskCreationOptions.LongRunning);
        }

		#endregion

		#region Async Stress Test

		[RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
		public async Task Async()
        {
            await RunStressTest(LaunchAsyncWorkers);
        }

        private IList<IAsyncCommand<TContext>> CreateAsyncCommands()
        {
            var result = new List<IAsyncCommand<TContext>>
            {
                new AsyncReadCommand<TContext>(_driver, false),
                new AsyncReadCommand<TContext>(_driver, true),
                new AsyncReadCommandInTx<TContext>(_driver, false),
                new AsyncReadCommandInTx<TContext>(_driver, true),
                new AsyncWriteCommand<TContext>(this, _driver, false),
                new AsyncWriteCommand<TContext>(this, _driver, true),
                new AsyncWriteCommandInTx<TContext>(this, _driver, false),
                new AsyncWriteCommandInTx<TContext>(this, _driver, true),
                new AsyncWrongCommand<TContext>(_driver),
                new AsyncWrongCommandInTx<TContext>(_driver),
                new AsyncFailingCommand<TContext>(_driver),
                new AsyncFailingCommandInTx<TContext>(_driver)
            };

            result.AddRange(CreateTestSpecificAsyncCommands());

            return result;
        }

        private IEnumerable<Task> LaunchAsyncWorkers(TContext context)
        {
            var commands = CreateAsyncCommands();

            var tasks = new List<Task>();
            for (var i = 0; i < StressTestThreadCount; i++)
            {
                tasks.Add(LaunchAsyncWorkerThread(context, commands));
            }

            return tasks;
        }

        private static Task LaunchAsyncWorkerThread(TContext context, IList<IAsyncCommand<TContext>> commands)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!context.Stopped)
                {
                    Task.WaitAll(Enumerable.Range(1, StressTestAsyncBatchSize)
                        .Select(_ => commands.RandomElement().ExecuteAsync(context))
                        .ToArray());
                }
            }, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Reactive Stress Test

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async Task Reactive()
        {
            await RunStressTest(LaunchRxWorkers);
        }

        private IList<IRxCommand<TContext>> CreateRxCommands()
        {
            var result = new List<IRxCommand<TContext>>
            {
                new RxReadCommand<TContext>(_driver, false),
                new RxReadCommand<TContext>(_driver, true),
                new RxReadCommandInTx<TContext>(_driver, false),
                new RxReadCommandInTx<TContext>(_driver, true),
                new RxWriteCommand<TContext>(this, _driver, false),
                new RxWriteCommand<TContext>(this, _driver, true),
                new RxWriteCommandInTx<TContext>(this, _driver, false),
                new RxWriteCommandInTx<TContext>(this, _driver, true),
                new RxWrongCommand<TContext>(_driver),
                new RxWrongCommandInTx<TContext>(_driver),
                new RxFailingCommand<TContext>(_driver),
                new RxFailingCommandInTx<TContext>(_driver)
            };

            result.AddRange(CreateTestSpecificRxCommands());

            return result;
        }

        private IEnumerable<Task> LaunchRxWorkers(TContext context)
        {
            var commands = CreateRxCommands();

            var tasks = new List<Task>();
            for (var i = 0; i < StressTestThreadCount; i++)
            {
                tasks.Add(LaunchRxWorkerThread(context, commands));
            }

            return tasks;
        }

        private static Task LaunchRxWorkerThread(TContext context, IList<IRxCommand<TContext>> commands)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!context.Stopped)
                {
                    Task.WaitAll(Enumerable.Range(1, StressTestAsyncBatchSize)
                        .Select(_ => commands.RandomElement().ExecuteAsync(context))
                        .ToArray());
                }
            }, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Async Big Data Tests

        [RequireServerFact]
        public async Task AsyncBigData()
        {
            var bookmark = await CreateNodesAsync(BigDataTestBatchCount, BigDataTestBatchSize, BigDataTestBatchBuffer,
                _driver);
            await ReadNodesAsync(_driver, bookmark, BigDataTestBatchCount * BigDataTestBatchSize);
        }

        private async Task<Bookmark> CreateNodesAsync(int batchCount, int batchSize, int batchBuffer, IDriver driver)
        {
            var timer = Stopwatch.StartNew();

            var session = driver.AsyncSession();
            try
            {
                for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
                {
                    await session.WriteTransactionAsync(txc => Task.WhenAll(
                        Enumerable.Range(1, batchSize)
                            .Select(index => (batchIndex * batchSize) + index)
                            .Batch(batchBuffer)
                            .Select(indices =>
                                txc.RunAsync(CreateBatchNodesQuery(indices))
                                    .ContinueWith(t => t.Result.ConsumeAsync()).Unwrap()).ToArray()));
                }
            }
            finally
            {
                await session.CloseAsync();
            }

            _output.WriteLine("Creating nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);

            return session.LastBookmark;
        }

        private Query CreateBatchNodesQuery(IEnumerable<int> batch)
        {
            return new Query("UNWIND $values AS props CREATE (n:Test:Node) SET n = props", new
            {
                values = batch.Select(nodeIndex => new
                {
                    index = nodeIndex,
                    name = $"name-{nodeIndex}",
                    surname = $"surname-{nodeIndex}",
                    longList = Enumerable.Repeat((long) nodeIndex, 10),
                    doubleList = Enumerable.Repeat((double) nodeIndex, 10),
                    boolList = Enumerable.Repeat(nodeIndex % 2 == 0, 10),
                })
            });
        }

        private async Task ReadNodesAsync(IDriver driver, Bookmark bookmark, int expectedNodes)
        {
            var timer = Stopwatch.StartNew();

            var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(bookmark));
            try
            {
                await session.ReadTransactionAsync(async txc =>
                {
                    var records = await txc.RunAsync("MATCH (n:Node) RETURN n ORDER BY n.index")
                        .ContinueWith(r => r.Result.ToListAsync())
                        .Unwrap();

                    records.Select(r => r[0].As<INode>())
                        .Should().BeEquivalentTo(
                            Enumerable.Range(1, expectedNodes).Select(index =>
                                new
                                {
                                    Labels = new[] {"Test", "Node"},
                                    Properties = new Dictionary<string, object>(6)
                                    {
                                        {"index", (long) index},
                                        {"name", $"name-{index}"},
                                        {"surname", $"surname-{index}"},
                                        {
                                            "longList",
                                            Enumerable.Repeat((long) index, 10)
                                        },
                                        {
                                            "doubleList",
                                            Enumerable.Repeat((double) index, 10)
                                        },
                                        {
                                            "boolList",
                                            Enumerable.Repeat(index % 2 == 0, 10)
                                        },
                                    }
                                }), opts => opts.Including(x => x.Labels).Including(x => x.Properties));
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

        [RequireServerFact]
        public void BlockingBigData()
        {
            var bookmark = CreateNodes(BigDataTestBatchCount, BigDataTestBatchSize, BigDataTestBatchBuffer,
                _driver);
            ReadNodes(_driver, bookmark, BigDataTestBatchCount * BigDataTestBatchSize);
        }

        private Bookmark CreateNodes(int batchCount, int batchSize, int batchBuffer, IDriver driver)
        {
            var timer = Stopwatch.StartNew();

            using (var session = driver.Session())
            {
                for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
                {
                    var index = batchIndex;

                    session.WriteTransaction(txc =>
                        Enumerable.Range(1, batchSize)
                            .Select(item => (index * batchSize) + item)
                            .Batch(batchBuffer).Select(indices => txc.Run(CreateBatchNodesQuery(indices)).Consume())
                            .ToArray());
                }

                _output.WriteLine("Creating nodes with Sync API took: {0}ms", timer.ElapsedMilliseconds);

                return session.LastBookmark;
            }
        }

        private void ReadNodes(IDriver driver, Bookmark bookmark, int expectedNodes)
        {
            var timer = Stopwatch.StartNew();

            using (var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(bookmark)))
            {
                session.ReadTransaction(txc =>
                {
                    var result = txc.Run("MATCH (n:Node) RETURN n ORDER BY n.index");

                    result.Select(r => r[0].As<INode>())
                        .Should().BeEquivalentTo(
                            Enumerable.Range(1, expectedNodes).Select(index =>
                                new
                                {
                                    Labels = new[] {"Test", "Node"},
                                    Properties = new Dictionary<string, object>(6)
                                    {
                                        {"index", (long) index},
                                        {"name", $"name-{index}"},
                                        {"surname", $"surname-{index}"},
                                        {
                                            "longList",
                                            Enumerable.Repeat((long) index, 10)
                                        },
                                        {
                                            "doubleList",
                                            Enumerable.Repeat((double) index, 10)
                                        },
                                        {
                                            "boolList",
                                            Enumerable.Repeat(index % 2 == 0, 10)
                                        },
                                    }
                                }), opts => opts.Including(x => x.Labels).Including(x => x.Properties));

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
            var bookmark = CreateNodesRx(BigDataTestBatchCount, BigDataTestBatchSize, BigDataTestBatchBuffer,
                _driver);
            ReadNodesRx(_driver, bookmark, BigDataTestBatchCount * BigDataTestBatchSize);
        }

        private Bookmark CreateNodesRx(int batchCount, int batchSize, int batchBuffer, IDriver driver)
        {
            var timer = Stopwatch.StartNew();
            var session = driver.RxSession();

            Observable.Range(0, batchCount).Select(batchIndex =>
                    session.WriteTransaction(txc => Observable.Range(1, batchSize)
                        .Select(index => (batchIndex * batchSize) + index)
                        .Buffer(batchBuffer)
                        .SelectMany(batch => txc.Run(CreateBatchNodesQuery(batch)).Consume())
                    ))
                .Concat()
                .Concat(session.Close<IResultSummary>()).CatchAndThrow(_ => session.Close<IResultSummary>())
                .Wait();

            _output.WriteLine("Creating nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);

            return session.LastBookmark;
        }

        private void ReadNodesRx(IDriver driver, Bookmark bookmark, int expectedNodes)
        {
            var timer = Stopwatch.StartNew();

            var session = driver.RxSession(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(bookmark));

            session.ReadTransaction(txc =>
                    txc.Run("MATCH (n:Node) RETURN n ORDER BY n.index").Records().Select(r => r[0].As<INode>()).Do(n =>
                    {
                        var index = n.Properties["index"].As<int>();

                        n.Should().BeEquivalentTo(
                            new
                            {
                                Labels = new[] {"Test", "Node"},
                                Properties = new Dictionary<string, object>(6)
                                {
                                    {"index", (long) index},
                                    {"name", $"name-{index}"},
                                    {"surname", $"surname-{index}"},
                                    {
                                        "longList",
                                        Enumerable.Repeat((long) index, 10)
                                    },
                                    {
                                        "doubleList",
                                        Enumerable.Repeat((double) index, 10)
                                    },
                                    {
                                        "boolList",
                                        Enumerable.Repeat(index % 2 == 0, 10)
                                    },
                                }
                            }, opts => opts.Including(x => x.Labels).Including(x => x.Properties));
                    }))
                .Concat(session.Close<INode>())
                .CatchAndThrow(_ => session.Close<INode>())
                .Wait();

            _output.WriteLine("Reading nodes with Async API took: {0}ms", timer.ElapsedMilliseconds);
        }

        #endregion

        #region Pooling Stress Tests

        [RequireServerFact]
        public void Pool()
        {
            var tokenSource = new CancellationTokenSource();
            var failure = new AtomicReference<Exception>(null);

            var tasks = LaunchPoolWorkers(_driver, tokenSource.Token, worker => worker.RunWithNoTx(), failure);
            Thread.Sleep(PoolTestDuration);
            tokenSource.Cancel();
            Task.WaitAll(tasks);

            failure.Get().Should().BeNull("Some workers have failed");
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

            failure.Get().Should().BeNull("Some workers have failed");
        }

        [RequireServerFact]
        public void PoolWithTxFuncWithFailingConnections()
        {
            var tokenSource = new CancellationTokenSource();
            var failure = new AtomicReference<Exception>(null);
            var (driver, connections) = SetupMonitoredDriver();

            var terminator = LaunchConnectionTerminator(connections, tokenSource.Token);
            var tasks = LaunchPoolWorkers(driver, tokenSource.Token, worker => worker.RunWithTxFunc(), failure);
            Thread.Sleep(PoolTestDuration);
            tokenSource.Cancel();
            Task.WaitAll(tasks.Union(new[] {terminator}).ToArray());

            failure.Get().Should().BeNull("no workers should fail");
        }


        private static Task[] LaunchPoolWorkers(IDriver driver, CancellationToken token, Action<Worker> job,
            AtomicReference<Exception> failure)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < PoolTestThreadCount; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var worker = new Worker(driver, token, failure);
                    job(worker);
                }, TaskCreationOptions.LongRunning));
            }

            return tasks.ToArray();
        }

        private (IDriver, ConcurrentQueue<IPooledConnection>) SetupMonitoredDriver()
        {
            var configBuilder = Config.Builder
                .WithMetricsEnabled(true)
                .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(5))
                .WithConnectionTimeout(Config.InfiniteInterval)
                .WithMaxConnectionPoolSize(100)
                .WithLogger(new StressTestLogger(_output, LoggingEnabled));
            _configure?.Invoke(configBuilder);
            var config = configBuilder.Build();

            var connectionSettings = new ConnectionSettings(_databaseUri, _authToken, config);
            var bufferSettings = new BufferSettings(config);
            var connectionFactory = new MonitoredPooledConnectionFactory(
                new PooledConnectionFactory(connectionSettings, bufferSettings, config.Logger));

            return ((Internal.Driver) GraphDatabase.CreateDriver(_databaseUri, config, connectionFactory),
                connectionFactory.Connections);
        }

        private Task LaunchConnectionTerminator(ConcurrentQueue<IPooledConnection> connections, CancellationToken token)
        {
            return Task.Factory.StartNew(async () =>
            {
                const int minimalConnCount = 3;
                while (!token.IsCancellationRequested)
                {
                    if (connections.Count > minimalConnCount && connections.TryDequeue(out var conn))
                    {
                        if (conn.Server.Version != null)
                        {
                            await conn.DestroyAsync();
                            _output.WriteLine($"Terminator killed connection {conn} towards server {conn.Server}");
                        }
                        else
                        {
                            // connection is still being initialized, put it back to the connections list
                            connections.Enqueue(conn);
                        }
                    }
                    else
                    {
                        _output.WriteLine("Terminator failed to find a open connection to kill.");
                    }

                    try
                    {
                        await Task.Delay(1000, token); // sleep
                    }
                    catch (TaskCanceledException)
                    {
                        // we are fine with cancelled sleep
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private class Worker
        {
            private
                static
                readonly
                (AccessMode, string)[]
                Queries =
                {
                    (AccessMode.Read, "RETURN 1295+42"), (AccessMode.Write,
                        "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n")
                };

            private readonly IDriver _driver;
            private readonly CancellationToken _token;
            private readonly Random _rnd = new Random();
            private readonly AtomicReference<Exception> _failure;

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
                            session.ReadTransaction(txc => Execute(txc, query));
                            break;
                        case AccessMode.Write:
                            session.WriteTransaction(txc => Execute(txc, query));
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
            public readonly ConcurrentQueue<IPooledConnection> Connections = new ConcurrentQueue<IPooledConnection>();

            public MonitoredPooledConnectionFactory(IPooledConnectionFactory factory)
            {
                _delegate = factory;
            }

            public IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager, IDictionary<string, string> routingContext)
            {
                var pooledConnection = _delegate.Create(uri, releaseManager, routingContext);
                Connections.Enqueue(pooledConnection);
                return pooledConnection;
            }
        }

        #endregion

        #region Test and Verifications

        private async Task RunStressTest(Func<TContext, IEnumerable<Task>> launcher)
        {
            var context = CreateContext();
            var workers = launcher(context);

            await Task.Delay(StressTestExecutionTime);
            context.Stop();

            await Task.WhenAll(workers);

            PrintStats(context);

            VerifyResults(context);
        }


        private void VerifyResults(TContext context)
        {
            VerifyNodesCreated(context.CreatedNodesCount);
            VerifyReadQueryDistribution(context);
        }

        private void VerifyNodesCreated(long expected)
        {
            using (var session = _driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
            {
                var count = session.Run("MATCH (n) RETURN count(n) as createdNodes")
                    .Select(r => r["createdNodes"].As<long>()).Single();

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
                return;

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

        private class StressTestLogger : ILogger
        {
            private readonly ITestOutputHelper _output;
            private readonly bool _enabled;

            public StressTestLogger(ITestOutputHelper output, bool enabled)
            {
                _output = output ?? throw new ArgumentNullException(nameof(output));
                _enabled = enabled;
            }

            private void Write(string message, params object[] args)
            {
                if (_enabled)
                {
                    _output.WriteLine(message, args);
                }
            }

            public void Error(Exception cause, string message, params object[] args)
            {
                Write(message, args);
            }

            public void Warn(Exception cause, string message, params object[] args)
            {
                Write(message, args);
            }

            public void Info(string message, params object[] args)
            {
                Write(message, args);
            }

            public void Debug(string message, params object[] args)
            {
                Write(message, args);
            }

            public void Trace(string message, params object[] args)
            {
                Write(message, args);
            }

            public bool IsTraceEnabled()
            {
                return false;
            }

            public bool IsDebugEnabled()
            {
                return true;
            }
        }
    }
}
