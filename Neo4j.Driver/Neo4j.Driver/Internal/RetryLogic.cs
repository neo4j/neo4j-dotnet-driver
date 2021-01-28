// Copyright (c) "Neo4j"
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal interface IRetryLogic
    {
        T Retry<T>(Func<T> runTxFunc);
        Task<T> RetryAsync<T>(Func<Task<T>> runTxAsyncFunc);
    }

    internal class ExponentialBackoffRetryLogic : IRetryLogic
    {
        private readonly double _maxRetryTimeMs;
        private readonly double _initialRetryDelayMs;
        private readonly double _multiplier;
        private readonly double _jitterFactor;

        private static readonly double InitialRetryDelayMs = TimeSpan.FromSeconds(1).TotalMilliseconds;
        private const double RetryDelayMultiplier = 2.0;
        private const double RetryDelayJitterFactor = 0.2;

        private readonly IDriverLogger _logger;

        public ExponentialBackoffRetryLogic(TimeSpan maxRetryTimeout, IDriverLogger logger)
        {
            _maxRetryTimeMs = maxRetryTimeout.TotalMilliseconds;
            _logger = logger;
            _initialRetryDelayMs = InitialRetryDelayMs;
            _multiplier = RetryDelayMultiplier;
            _jitterFactor = RetryDelayJitterFactor;
        }

        public T Retry<T>(Func<T> runTxFunc)
        {
            var exceptions = new List<Exception>();
            var timer = new Stopwatch();
            var delay = TimeSpan.Zero;
            var delayMs = _initialRetryDelayMs;
            var retryCount = 0;
            var shouldRetry = false;

            timer.Start();
            do
            {
                retryCount++;
                try
                {
                    return runTxFunc();
                }
                catch (Exception e) when (e.IsRetriableError())
                {
                    exceptions.Add(e);

                    // we want the retry to happen at least twice and as much as the max retry time allows 
                    shouldRetry = retryCount < 2 || timer.ElapsedMilliseconds < _maxRetryTimeMs;

                    if (shouldRetry)
                    {
                        delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                        _logger?.Warn(e, $"Transaction failed and will be retried in {delay}ms.");
                        Thread.Sleep(delay);
                        delayMs *= _multiplier;
                    }
                }
            } while (shouldRetry);

            timer.Stop();
            throw new ServiceUnavailableException(
                $"Failed after retried for {retryCount} times in {_maxRetryTimeMs}ms. " +
                "Make sure that your database is online and retry again.", new AggregateException(exceptions));
        }

        public async Task<T> RetryAsync<T>(Func<Task<T>> runTxAsyncFunc)
        {
            var exceptions = new List<Exception>();
            var timer = new Stopwatch();
            var delay = TimeSpan.Zero;
            var delayMs = _initialRetryDelayMs;
            var retryCount = 0;
            var shouldRetry = false;

            timer.Start();
            do
            {
                retryCount++;
                try
                {
                    return await runTxAsyncFunc().ConfigureAwait(false);
                }
                catch (Exception e) when (e.IsRetriableError())
                {
                    exceptions.Add(e);

                    // we want the retry to happen at least twice and as much as the max retry time allows 
                    shouldRetry = retryCount < 2 || timer.ElapsedMilliseconds < _maxRetryTimeMs;

                    if (shouldRetry)
                    {
                        delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                        _logger?.Warn(e, $"Transaction failed and will be retried in {delay} ms.");
                        await Task.Delay(delay).ConfigureAwait(false); // blocking for this delay
                        delayMs *= _multiplier;
                    }
                }
            } while (shouldRetry);

            timer.Stop();
            throw new ServiceUnavailableException(
                $"Failed after retried for {retryCount} times in {_maxRetryTimeMs} ms. " +
                "Make sure that your database is online and retry again.", new AggregateException(exceptions));
        }


        private double ComputeDelayWithJitter(double delayMs)
        {
            var jitter = delayMs * _jitterFactor;
            return delayMs - jitter + 2 * jitter * new Random(Guid.NewGuid().GetHashCode()).NextDouble();
        }
    }
}