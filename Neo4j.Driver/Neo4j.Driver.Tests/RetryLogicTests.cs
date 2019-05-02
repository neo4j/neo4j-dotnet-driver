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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.Internal.ErrorExtensions;

namespace Neo4j.Driver.Tests
{
    public class RetryLogicTests
    {
        private readonly ITestOutputHelper _output;
        private long _globalCounter;

        public RetryLogicTests(ITestOutputHelper output)
        {
            _output = output;
            _globalCounter = 0;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(20)]
        public void ShouldRetry(int numberOfParallelRetries)
        {
            var mockLogger = new Mock<IDriverLogger>();

            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(5), mockLogger.Object);
            Parallel.For(0, numberOfParallelRetries, i => Retry(i, retryLogic));

            mockLogger.Verify(l => l.Warn(It.IsAny<Exception>(), It.IsAny<string>()),
                Times.Exactly((int) Interlocked.Read(ref _globalCounter)));
        }

        private void Retry(int index, IRetryLogic retryLogic)
        {
            var timer = new Stopwatch();
            timer.Start();
            var runCounter = 0;
            var e = Record.Exception(() => retryLogic.Retry<int>(() =>
            {
                runCounter++;
                Interlocked.Increment(ref _globalCounter);
                var errorMessage = $"Thread {index} Failed at {timer.Elapsed}";
                throw new SessionExpiredException(errorMessage);
            }));
            timer.Stop();

            e.Should().BeOfType<ServiceUnavailableException>()
                .Which.InnerException.Should().BeOfType<AggregateException>()
                .Which.Flatten().InnerExceptions.Should().HaveCount(runCounter);
            timer.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(5);
        }

        [Theory]
        [InlineData("Neo.TransientError.Transaction.Terminated")]
        [InlineData("Neo.TransientError.Transaction.LockClientStopped")]
        public void ShouldNotRetryOnError(string errorCode)
        {
            var mockLogger = new Mock<IDriverLogger>();
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(30), mockLogger.Object);

            int count = 0;
            var e = Record.Exception(() => retryLogic.Retry<int>(() =>
            {
                count++;
                throw ParseServerException(errorCode, "an error");
            }));

            e.Should().BeOfType<TransientException>().Which.Code.Should().Be(errorCode);
            count.Should().Be(1);
            mockLogger.Verify(l => l.Warn(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ShouldRetryEvenOriginalTaskTakesLongerThanMaxRetryDuration()
        {
            var logger = new Mock<IDriverLogger>();
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(2), logger.Object);

            var counter = 0;
            var result = retryLogic.Retry(() =>
            {
                if (counter == 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    counter++;
                    throw new SessionExpiredException("session expired");
                }

                return counter;
            });

            result.Should().Be(1);
            logger.Verify(
                l => l.Warn(It.IsAny<SessionExpiredException>(),
                    It.Is<string>(s => s.StartsWith("Transaction failed and will be retried"))), Times.Once);
        }

        [Fact]
        public async Task ShouldRetryEvenOriginalTaskTakesLongerThanMaxRetryDurationAsync()
        {
            var logger = new Mock<IDriverLogger>();
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(2), logger.Object);

            var counter = 0;
            var result = await retryLogic.RetryAsync(async () =>
            {
                if (counter == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    counter++;
                    throw new SessionExpiredException("session expired");
                }

                return counter;
            });

            result.Should().Be(1);
            logger.Verify(
                l => l.Warn(It.IsAny<SessionExpiredException>(),
                    It.Is<string>(s => s.StartsWith("Transaction failed and will be retried"))), Times.Once);
        }
    }
}