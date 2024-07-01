// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Metrics;

/// <summary>The driver metrics</summary>
internal interface IMetrics
{
    /// <summary>The connection pool metrics.</summary>
    IDictionary<string, IConnectionPoolMetrics> ConnectionPoolMetrics { get; }
}

/// <summary>The connection pool metrics</summary>
internal interface IConnectionPoolMetrics
{
    /// <summary>
    /// The unique name of this metrics, used as an unique identifier among all <see cref="IConnectionPoolMetrics"/>
    /// instances.
    /// </summary>
    string Id { get; }

    /// <summary>The amount of the connections that are used by user's application</summary>
    int InUse { get; }

    /// <summary>The amount of connections that are buffered by the pool</summary>
    int Idle { get; }

    /// <summary>The amount of connections that are waiting to be created.</summary>
    int Creating { get; }

    /// <summary>The amount of connections that have been created by this driver</summary>
    long Created { get; }

    /// <summary>
    /// The amount of connections that are failed to be created. The cause of the error could be pool is full for
    /// example.
    /// </summary>
    long FailedToCreate { get; }

    /// <summary>The amount of connections that are waiting to be closed.</summary>
    int Closing { get; }

    /// <summary>The amount of connections that have been closed by this driver.</summary>
    long Closed { get; }

    /// <summary>The amount of requests trying to acquire a connection from the pool.</summary>
    int Acquiring { get; }

    /// <summary>The amount of requests that have acquired a connection out of the pool.</summary>
    long Acquired { get; }

    /// <summary>The amount of requests to acquire a connection from pool but failed due to acquisition timeout.</summary>
    long TimedOutToAcquire { get; }
}
