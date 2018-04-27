// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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

        public RetryLogicTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(20)]
        public void ShouldRetry(int index)
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger.SetupGet(l => l.Level).Returns(LogLevel.Info);
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(5), mockLogger.Object);
            Parallel.For(0, index, i => Retry(i, retryLogic));

            mockLogger.Verify(l => l.Info(It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeast(2 * index));
        }

        private void Retry(int index, IRetryLogic retryLogic)
        {
            var timer = new Stopwatch();
            timer.Start();
            var runCounter = 0;
            var e = Record.Exception(() => retryLogic.Retry<int>(() =>
            {
                runCounter++;
                var errorMessage = $"Thread {index} Failed at {timer.Elapsed}";
                throw new SessionExpiredException(errorMessage);
            }));
            timer.Stop();

            e.Should().BeOfType<ServiceUnavailableException>();
            var error = e.InnerException as AggregateException;
            var innerErrors = error.Flatten().InnerExceptions;

            innerErrors.Count.Should().Be(runCounter);
            timer.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(5);
        }

        [Theory]
        [InlineData("Neo.TransientError.Transaction.Terminated")]
        [InlineData("Neo.TransientError.Transaction.LockClientStopped")]
        public void ShouldNotRetryOnError(string errorCode)
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger.SetupGet(l => l.Level).Returns(LogLevel.Info);
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(30), mockLogger.Object);
            var timer = new Stopwatch();
            timer.Start();
            var e = Record.Exception(() => retryLogic.Retry<int>(() =>
            {
                throw ParseServerException(errorCode, "an error");
            }));
            timer.Stop();
            e.Should().BeOfType<TransientException>();
            (e as TransientException).Code.Should().Be(errorCode);
            timer.Elapsed.TotalMilliseconds.Should().BeLessThan(10);
            mockLogger.Verify(l => l.Info(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }
    }
}
