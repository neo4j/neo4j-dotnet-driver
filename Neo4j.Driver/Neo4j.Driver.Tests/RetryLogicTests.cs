// Copyright (c) 2002-2018 "Neo4j,"
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
        public void ShouldRetry(int index)
        {
            var mockLogging = new Mock<ILogging>();
            var mockLogger = new Mock<IDriverLogger>();
            mockLogging.Setup(x => x.GetLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(5), mockLogging.Object);
            Parallel.For(0, index, i => Retry(i, retryLogic));

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
            var mockLogging = new Mock<ILogging>();
            var mockLogger = new Mock<IDriverLogger>();
            mockLogging.Setup(x => x.GetLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(30), mockLogging.Object);

            int count = 0;
            var e = Record.Exception(() => retryLogic.Retry<int>(() =>
            {
                count++;
                throw ParseServerException(errorCode, "an error");
            }));

            e.Should().BeOfType<TransientException>();
            (e as TransientException).Code.Should().Be(errorCode);
            count.Should().Be(1);
            mockLogger.Verify(l => l.Warn(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }
    }
}
