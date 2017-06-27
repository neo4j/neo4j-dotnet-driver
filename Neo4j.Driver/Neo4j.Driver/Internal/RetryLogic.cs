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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        private readonly ILogger _logger;

        public ExponentialBackoffRetryLogic(Config config)
            :this(config.MaxTransactionRetryTime, config.Logger)
        {
        }

        public ExponentialBackoffRetryLogic(TimeSpan maxRetryTimeout, ILogger logger = null)
        {
            _maxRetryTimeMs = maxRetryTimeout.TotalMilliseconds;
            _logger = logger;
            _initialRetryDelayMs = InitialRetryDelayMs;
            _multiplier = RetryDelayMultiplier;
            _jitterFactor = RetryDelayJitterFactor;
        }

        public T Retry<T>(Func<T> runTxFunc)
        {
            AggregateException exception = null;
            var timer = new Stopwatch();
            timer.Start();
            var delayMs = _initialRetryDelayMs;
            do
            {
                try
                {
                    return runTxFunc();
                }
                catch (Exception e) when (e is SessionExpiredException || e.IsRetriableTransientError() || e is ServiceUnavailableException)
                {
                    exception = exception == null ? new AggregateException(e) : new AggregateException(exception, e);

                    var delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                    _logger?.Info("Transaction failed and will be retried in " + delay + "ms.", e);
                    Task.Delay(delay).Wait(); // blocking for this delay
                    delayMs = delayMs * _multiplier;
                }
            } while (timer.Elapsed.TotalMilliseconds < _maxRetryTimeMs);

            timer.Stop();
            throw exception;
        }

        public async Task<T> RetryAsync<T>(Func<Task<T>> runTxAsyncFunc)
        {
            AggregateException exception = null;
            var timer = new Stopwatch();
            timer.Start();
            var delayMs = _initialRetryDelayMs;
            do
            {
                try
                {
                    return await runTxAsyncFunc().ConfigureAwait(false);
                }
                catch (Exception e) when (e is SessionExpiredException || e.IsRetriableTransientError() || e is ServiceUnavailableException)
                {
                    exception = exception == null ? new AggregateException(e) : new AggregateException(exception, e);

                    var delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                    _logger?.Info("Transaction failed and will be retried in " + delay + "ms.", e);
                    Task.Delay(delay).Wait(); // blocking for this delay
                    delayMs = delayMs * _multiplier;
                }
            } while (timer.Elapsed.TotalMilliseconds < _maxRetryTimeMs);

            timer.Stop();
            throw exception;
        }

        private double ComputeDelayWithJitter(double delayMs)
        {
            var jitter = delayMs * _jitterFactor;
            return delayMs - jitter + 2 * jitter * new Random(Guid.NewGuid().GetHashCode()).NextDouble();
        }
    }
}
