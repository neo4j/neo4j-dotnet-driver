// Copyright (c) 2002-2018 "Neo Technology,"
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

namespace Neo4j.Driver.Internal.Metrics
{
    /// <summary>
    /// The driver metrics
    /// </summary>
    internal interface IMetrics
    {
        /// <summary>
        /// The connection pool metrics.
        /// </summary>
        IDictionary<string, IConnectionPoolMetrics> ConnectionPoolMetrics { get; }

        /// <summary>
        /// The connection metrics.
        /// </summary>
        IDictionary<string, IConnectionMetrics> ConnectionMetrics { get; }
    }

    internal interface IConnectionMetrics
    {
        /// <summary>
        /// The unique name of this metrics, used as an unique identifier among all <see cref="IConnectionMetrics"/> instances.
        /// </summary>
        string UniqueName { get; }

        /// <summary>
        /// Records the distribution of connection establish time in "ticks" where a tick equals to 100 ns.
        /// </summary>
        IHistogram ConnectionTimeHistogram { get; }

        /// <summary>
        /// Records the distribution of the time that connections are borrowed out of the pool.
        /// The value are recorded in "ticks" where a tick equals to 100 ns.
        /// </summary>
        IHistogram InUseTimeHistogram { get; }
    }

    /// <summary>
    /// The connection pool metrics
    /// </summary>
    internal interface IConnectionPoolMetrics
    {
        /// <summary>
        /// The unique name of this metrics, used as an unique identifier among all <see cref="IConnectionPoolMetrics"/> instances.
        /// </summary>
        string UniqueName { get; }

        /// <summary>
        /// The pool status
        /// </summary>
        PoolStatus PoolStatus { get; }

        /// <summary>
        /// The amount of the connections that are used by user's application
        /// </summary>
        int InUse { get; }

        /// <summary>
        /// The amount of connections that are buffered by the pool
        /// </summary>
        int Idle { get; }

        /// <summary>
        /// The amount of connections that are waiting to be created.
        /// </summary>
        int Creating { get; }

        /// <summary>
        /// The amount of connections that have been created by this driver
        /// </summary>
        long Created { get; }

        /// <summary>
        /// The amount of connections that are failed to be created.
        /// The cause of the error could be pool is full for example.
        /// </summary>
        long FailedToCreate { get; }

        /// <summary>
        /// The amount of connections that are waiting to be closed.
        /// </summary>
        int Closing { get; }

        /// <summary>
        /// The amount of connections that have been closed by this driver.
        /// </summary>
        long Closed { get; }

        /// <summary>
        /// The amount of requests trying to acquire a connection from the pool.
        /// </summary>
        int Acquiring { get; }

        /// <summary>
        /// The amount of requests that have acquired a connection out of the pool.
        /// </summary>
        long Acquired { get; }

        /// <summary>
        /// The amount of requests to acquire a connection from pool but failed due to acquisition timeout.
        /// </summary>
        long TimedOutToAcquire { get; }

        /// <summary>
        /// The histogram of the delays to acquire a connection from the pool in "ticks" where a tick equals to 100 ns.
        /// The delays could either be the time to create a new connection or the time waiting for a connection available from the pool.
        /// </summary>
        IHistogram AcquisitionTimeHistogram { get; }
    }

    internal enum PoolStatus
    {
        Open, Closed, Inactive
    }

    /// <summary>
    /// A very simple histogram interface
    /// </summary>
    internal interface IHistogram
    {
        /// <summary>
        /// Max value
        /// </summary>
        long Max { get; }
        /// <summary>
        /// Mean value. If there is no values recorded in this histogram, a <see cref="double.NaN"/> will be return
        /// </summary>
        double Mean { get; }
        /// <summary>
        /// The standard deviation.
        /// If there is no values recorded in this histogram, a <see cref="double.NaN"/> will be return.
        /// </summary>
        double StdDeviation { get; }
        /// <summary>
        /// Total number of recorded values
        /// </summary>
        long TotalCount { get; }
        /// <summary>
        /// Get the value at a given percentile.
        /// Throws <see cref="ArgumentOutOfRangeException"/> when querying value on an empty histogram.
        /// </summary>
        /// <param name="percentile">The given percentile</param>
        /// <returns>The value at a given percentile</returns>
        long GetValueAtPercentile(double percentile);
        /// <summary>
        /// Reset the histogram content
        /// </summary>
        void Reset();
    }
}
