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
using System.Reactive.Linq;

namespace Neo4j.Driver.Internal
{
    internal interface IRxRetryLogic
    {
        IObservable<T> Retry<T>(IObservable<T> work);
    }

    internal class RxRetryLogic : IRxRetryLogic
    {
        private readonly int _maxRetryTimeout;
        private readonly double _initialDelay;
        private readonly double _delayMultiplier;
        private readonly double _delayJitter;
        private readonly Random _random;
        private readonly ILogger _logger;

        public RxRetryLogic(TimeSpan maxRetryTimeout, ILogger logger)
        {
            _maxRetryTimeout = (int) maxRetryTimeout.TotalMilliseconds;
            _initialDelay = TimeSpan.FromSeconds(1).TotalMilliseconds;
            _delayMultiplier = 2.0;
            _delayJitter = 0.2;
            _random = new Random(Guid.NewGuid().GetHashCode());
            _logger = logger;
        }

        public IObservable<T> Retry<T>(IObservable<T> work)
        {
            return work.RetryWhen(failedWork =>
            {
                var handledExceptions = new List<Exception>();
                var timer = Stopwatch.StartNew();
                var delay = _initialDelay;
                var retryCount = 1;

                return failedWork.SelectMany(exc =>
                {
                    if (!exc.CanBeRetried())
                    {
                        return Observable.Throw<int>(exc);
                    }

                    handledExceptions.Add(exc);

                    if (retryCount >= 2 && timer.ElapsedMilliseconds >= _maxRetryTimeout)
                    {
                        return Observable.Throw<int>(new ServiceUnavailableException(
                            $"Failed after retried for {retryCount} times in {_maxRetryTimeout} ms. " +
                            "Make sure that your database is online and retry again.",
                            new AggregateException(handledExceptions)));
                    }

                    var delayDuration = TimeSpan.FromMilliseconds(ComputeNextDelay(delay));
                    delay *= _delayMultiplier;
                    retryCount++;
                    _logger?.Warn(exc, $"Transaction failed and will be retried in {delay} ms.");
                    return Observable.Return(1).Delay(delayDuration);
                });
            });
        }

        private double ComputeNextDelay(double delay)
        {
            var jitter = delay * _delayJitter;
            return delay - jitter + (2 * jitter * _random.NextDouble());
        }
    }
}