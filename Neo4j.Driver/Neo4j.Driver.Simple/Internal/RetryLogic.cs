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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Neo4j.Driver.Internal;

internal interface IRetryLogic
{
    T Retry<T>(Func<T> work);
}

internal class RetryLogic : IRetryLogic
{
    private readonly double _delayJitter;
    private readonly double _delayMultiplier;
    private readonly double _initialDelay;
    private readonly ILogger _logger;
    private readonly int _maxRetryTimeout;
    private readonly Random _random;

    public RetryLogic(TimeSpan maxRetryTimeout, ILogger logger)
    {
        _maxRetryTimeout = (int)maxRetryTimeout.TotalMilliseconds;
        _initialDelay = TimeSpan.FromSeconds(1).TotalMilliseconds;
        _delayMultiplier = 2.0;
        _delayJitter = 0.2;
        _random = new Random(Guid.NewGuid().GetHashCode());
        _logger = logger;
    }

    public T Retry<T>(Func<T> work)
    {
        var exceptions = new List<Exception>();
        var delay = TimeSpan.Zero;
        var delayMs = _initialDelay;
        var retryCount = 0;
        var shouldRetry = false;

        var timer = Stopwatch.StartNew();
        do
        {
            retryCount++;
            try
            {
                return work();
            }
            catch (Exception e) when (e.CanBeRetried())
            {
                exceptions.Add(e);

                // we want the retry to happen at least twice and as much as the max retry time allows 
                shouldRetry = retryCount < 2 || timer.ElapsedMilliseconds < _maxRetryTimeout;

                if (shouldRetry)
                {
                    delay = TimeSpan.FromMilliseconds(ComputeNextDelay(delayMs));
                    _logger?.Warn(e, $"Transaction failed and will be retried in {delay}ms.");
                    Thread.Sleep(delay);
                    delayMs *= _delayMultiplier;
                }
            }
        } while (shouldRetry);

        timer.Stop();
        throw new ServiceUnavailableException(
            $"Failed after retried for {retryCount} times in {_maxRetryTimeout}ms. " +
            "Make sure that your database is online and retry again.",
            new AggregateException(exceptions));
    }

    private double ComputeNextDelay(double delay)
    {
        var jitter = delay * _delayJitter;
        return delay - jitter + 2 * jitter * _random.NextDouble();
    }
}
