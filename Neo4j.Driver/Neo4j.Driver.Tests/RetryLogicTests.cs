// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

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
            var retryLogic = new ExponentialBackoffRetryLogic(TimeSpan.FromSeconds(30));
            Parallel.For(0, index, i=>Retry(i, retryLogic));
        }

        private void Retry(int index, IRetryLogic retryLogic)
        {
            var timer = new Stopwatch();
            timer.Start();
            var e = Record.Exception(() => retryLogic.Retry(() =>
            {
                var errorMessage = $"Thread {index} Failed at {timer.Elapsed}";
                throw new SessionExpiredException(errorMessage);
                return timer.Elapsed.TotalMilliseconds;
            }));
            timer.Stop();

            var error = e as AggregateException;
            var innerErrors = error.Flatten().InnerExceptions;

            innerErrors.Count.Should().BeGreaterOrEqualTo(5);
            timer.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(30);
        }
    }
}