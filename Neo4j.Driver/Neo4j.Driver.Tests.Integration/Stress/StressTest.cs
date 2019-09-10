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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public abstract class StressTest<TContext> : IDisposable
        where TContext : StressTestContext
    {
        private const int ThreadCount = 8;
        private const int AsyncBatchSize = 10;
        private const bool LoggingEnabled = false;
        private static readonly TimeSpan ExecutionTime = TimeSpan.FromSeconds(30);

        protected readonly ITestOutputHelper _output;
        protected readonly IDriver _driver;

        protected StressTest(ITestOutputHelper output, Uri databaseUri, IAuthToken authToken)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _driver = GraphDatabase.Driver(databaseUri, authToken,
                Config.Builder.WithDriverLogger(new StressTestLogger(_output, LoggingEnabled))
                    .WithMaxConnectionPoolSize(100).WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(1))
                    .ToConfig());

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

        [RequireServerFact]
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
            for (var i = 0; i < ThreadCount; i++)
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

        [RequireServerFact]
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
            for (var i = 0; i < ThreadCount; i++)
            {
                tasks.Add(LaunchAsyncWorkerThread(context, commands));
            }

            return tasks;
        }

        private static Task LaunchAsyncWorkerThread(TContext context, IList<IAsyncCommand<TContext>> commands)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (!context.Stopped)
                {
                    await Task.WhenAll(Enumerable.Range(1, AsyncBatchSize)
                        .Select(_ => commands.RandomElement().ExecuteAsync(context))
                        .ToArray());
                }
            }, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Reactive Stress Test

        [RequireServerFact]
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
            for (var i = 0; i < ThreadCount; i++)
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
                    Task.WaitAll(Enumerable.Range(1, AsyncBatchSize)
                        .Select(_ => commands.RandomElement().ExecuteAsync(context))
                        .ToArray());
                }
            }, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Test and Verifications

        private async Task RunStressTest(Func<TContext, IEnumerable<Task>> launcher)
        {
            var context = CreateContext();
            var workers = launcher(context);

            await Task.Delay(ExecutionTime);
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
            CleanupDatabase();
        }

        private void CleanupDatabase()
        {
            using (var session = _driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
            {
                session.Run("MATCH (n) DETACH DELETE n").Consume();
            }
        }

        #endregion

        private class StressTestLogger : IDriverLogger
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
                Write(string.Format($"{message} - caused by{Environment.NewLine}{0}", cause), args);
            }

            public void Warn(Exception cause, string message, params object[] args)
            {
                Write(string.Format($"{message} - caused by{Environment.NewLine}{0}", cause), args);
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