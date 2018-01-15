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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Metrics
{
    /// <summary>
    /// The driver metrics
    /// </summary>
    internal interface IDriverMetrics
    {
        /// <summary>
        /// The connection pool metrics.
        /// Example: metrics.PoolMetrics[Name].Status
        /// </summary>
        IDictionary<string, IConnectionPoolMetrics> PoolMetrics { get; }

        // The connection metrics.
        // Example: metrics.ConnectionMetrics[Name].CreationTimeHistogram
        IDictionary<string, IConnectionMetrics> ConnectionMetrics { get; }
    }

    internal interface IConnectionMetrics
    {
        string UniqueName { get; }
        IHistogram ConnectionTimeHistogram { get; }
        IHistogram InUseTimeHistogram { get; }
    }

    /// <summary>
    /// The connection pool metrics
    /// </summary>
    internal interface IConnectionPoolMetrics
    {
        /// <summary>
        /// The unique name of this metrics, used as an unique identifier for this metrics.
        /// </summary>
        string UniqueName { get; }

        /// <summary>
        /// The pool status
        /// </summary>
        string Status { get; }

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
        int ToCreate { get; }

        /// <summary>
        /// The amount of connections that are waiting to be closed.
        /// </summary>
        int ToClose { get; }

        /// <summary>
        /// The amount of connections that have been created by this driver
        /// </summary>
        long Created { get; }

        /// <summary>
        /// The amount of connections that have been closed by this driver.
        /// </summary>
        long Closed { get; }

        /// <summary>
        /// The amount of connections that are failed to be created.
        /// The cause of the error could be pool is full for example.
        /// </summary>
        long FailedToCreate { get; }

        /// <summary>
        /// The histgram of the delays to acquire a connection from the pool in nano seconds.
        /// The delays could either be the time to create a new connection or the time waiting for a connection available from the pool.
        /// </summary>
        IHistogram AcuisitionTimeHistogram { get; }
    }

    /// <summary>
    /// A very simple histgram interface
    /// </summary>
    internal interface IHistogram
    {
        /// <summary>
        /// Max value
        /// </summary>
        long Max { get; }
        /// <summary>
        /// Mean value
        /// </summary>
        double Mean { get; }
        /// <summary>
        /// The standard deviation
        /// </summary>
        double StdDeviation { get; }
        /// <summary>
        /// Total count of values
        /// </summary>
        double TotalCount { get; }
        /// <summary>
        /// Get the value at a given percentile
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
