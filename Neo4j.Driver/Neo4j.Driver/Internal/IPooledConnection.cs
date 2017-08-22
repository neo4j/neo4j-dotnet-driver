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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal
{
    internal interface IPooledConnection : IConnection
    {
        /// <summary>
        /// An identifer of this connection for pooling
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Try to reset the connection to a clean state to prepare it for a new session.
        /// </summary>
        void ClearConnection();

        Task ClearConnectionAsync();

        ITimer IdleTimer { get; }

        ITimer LifetimeTimer { get; }
    }

    internal interface ITimer
    {
        /// <summary>Gets the total elapsed time measured by the current instance, in milliseconds.</summary>
        /// <returns>A read-only long integer representing the total number of milliseconds measured by the current instance.</returns>
        /// <filterpriority>1</filterpriority>
        long ElapsedMilliseconds { get; }

        /// <summary>Stops time interval measurement and resets the elapsed time to zero.</summary>
        void Reset();
        /// <summary>Starts, or resumes, measuring elapsed time for an interval.</summary>
        void Start();
    }
}
