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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class AsyncRetryLogicTests
    {
        [Theory]
        [MemberData(nameof(NonTransientErrors))]
        public async Task ShouldNotRetryOnNonTransientErrors(Exception error)
        {
            var retryLogic = new AsyncRetryLogic(TimeSpan.FromSeconds(5), new TestLogger(Console.WriteLine));
            var work = CreateFailingWork(0, error);

            var exc = await Record.ExceptionAsync(() => retryLogic.RetryAsync(() => work.Work(null)));

            exc.Should().Be(error);
            work.Invocations.Should().Be(1);
        }

        [Theory]
        [MemberData(nameof(TransientErrors))]
        public async Task ShouldRetryOnTransientErrors(Exception error)
        {
            var retryLogic = new AsyncRetryLogic(TimeSpan.FromSeconds(5), NullLogger.Instance);
            var work = CreateFailingWork(5, error);

            var result = await retryLogic.RetryAsync(() => work.Work(null));

            result.Should().Be(5);
            work.Invocations.Should().Be(2);
        }

        [Fact]
        public async Task ShouldNotRetryOnSuccess()
        {
            var retryLogic = new AsyncRetryLogic(TimeSpan.FromSeconds(5), NullLogger.Instance);
            var work = CreateFailingWork(5);

            var result = await retryLogic.RetryAsync(() => work.Work(null));

            result.Should().Be(5);
            work.Invocations.Should().Be(1);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public async Task ShouldLogRetries(int errorCount)
        {
            var error = new TransientException("code", "message");
            var logger = new Mock<ILogger>();
            var retryLogic = new AsyncRetryLogic(TimeSpan.FromMinutes(1), logger.Object);
            var work = CreateFailingWork(
                1,
                Enumerable.Range(1, errorCount).Select(x => error).Cast<Exception>().ToArray());

            var result = await retryLogic.RetryAsync(() => work.Work(null));

            result.Should().Be(1);
            logger.Verify(
                x => x.Warn(
                    error,
                    It.Is<string>(s => s.StartsWith("Transaction failed and will be retried in"))),
                Times.Exactly(errorCount));
        }

        [Fact]
        public async Task ShouldRetryAtLeastTwice()
        {
            var error = new TransientException("code", "message");
            var logger = new Mock<ILogger>();
            var retryLogic = new AsyncRetryLogic(TimeSpan.FromSeconds(1), logger.Object);
            var work = CreateFailingWork(TimeSpan.FromSeconds(2), 1, error);

            var result = await retryLogic.RetryAsync(() => work.Work(null));

            result.Should().Be(1);
            logger.Verify(
                x => x.Warn(
                    error,
                    It.Is<string>(s => s.StartsWith("Transaction failed and will be retried in"))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowServiceUnavailableWhenRetriesTimedOut()
        {
            var errorCount = 3;
            var exceptions = Enumerable.Range(1, errorCount)
                .Select(i => new TransientException($"{i}", $"{i}"))
                .Cast<Exception>()
                .ToArray();

            var logger = new Mock<ILogger>();
            var retryLogic = new AsyncRetryLogic(TimeSpan.FromSeconds(2), logger.Object);
            var work = CreateFailingWork(TimeSpan.FromSeconds(1), 1, exceptions);

            var exc = await Record.ExceptionAsync(() => retryLogic.RetryAsync(() => work.Work(null)));

            exc.Should()
                .BeOfType<ServiceUnavailableException>()
                .Which.InnerException.Should()
                .BeOfType<AggregateException>()
                .Which.InnerExceptions.Should()
                .BeSubsetOf(exceptions);
        }

        private static ConfigurableTransactionWork<T> CreateFailingWork<T>(T success, params Exception[] exceptions)
        {
            return CreateFailingWork(TimeSpan.Zero, success, exceptions);
        }

        private static ConfigurableTransactionWork<T> CreateFailingWork<T>(
            TimeSpan delay,
            T success,
            params Exception[] exceptions)
        {
            return new ConfigurableTransactionWork<T>(delay, success)
            {
                Failures = exceptions
            };
        }

        public static TheoryData<Exception> NonTransientErrors()
        {
            return new TheoryData<Exception>
            {
                new ArgumentOutOfRangeException("error"),
                new ClientException("invalid"),
                new InvalidOperationException("invalid operation"),
                new DatabaseException("Neo.TransientError.Transaction.Terminated", "transaction terminated"),
                new DatabaseException("Neo.TransientError.Transaction.LockClientStopped", "lock client stopped")
            };
        }

        public static TheoryData<Exception> TransientErrors()
        {
            return new TheoryData<Exception>
            {
                new TransientException("Neo.TransientError.Database.Unavailable", "database unavailable"),
                new SessionExpiredException("session expired"),
                new ServiceUnavailableException("service unavailable")
            };
        }

        private class ConfigurableTransactionWork<T>
        {
            private readonly TimeSpan _delay;
            private readonly T _result;
            private IEnumerator<Exception> _failures;
            private int _invocations;

            public ConfigurableTransactionWork(TimeSpan delay, T result)
            {
                _delay = delay;
                _result = result;
                _invocations = 0;
                _failures = Enumerable.Empty<Exception>().GetEnumerator();
            }

            public int Invocations => _invocations;

            public IEnumerable<Exception> Failures
            {
                set => _failures = (value ?? Enumerable.Empty<Exception>()).GetEnumerator();
            }

            public Task<T> Work(IAsyncTransaction txc)
            {
                Interlocked.Increment(ref _invocations);

                Thread.Sleep(_delay);

                if (_failures.MoveNext())
                {
                    return Task.FromException<T>(_failures.Current);
                }

                return Task.FromResult(_result);
            }
        }
    }
}
