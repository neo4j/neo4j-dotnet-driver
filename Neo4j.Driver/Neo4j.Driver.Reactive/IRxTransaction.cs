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

namespace Neo4j.Driver
{
    /// <summary>
    /// A reactive transaction, which provides the same functionality as <see cref="IAsyncTransaction"/>
    /// but with reactive API.
    /// </summary>
    public interface IRxTransaction : IRxRunnable
    {
        /// <summary>
        /// Commits the transaction and returns an empty reactive stream.
        /// 
        /// The type parameter makes it easier to chain this method to other reactive streams.
        /// </summary>
        /// <typeparam name="T">the desired return type</typeparam>
        /// <returns>an empty reactive stream</returns>
        IObservable<T> Commit<T>();

        /// <summary>
        /// Rollbacks the transaction and returns an empty reactive stream.
        /// 
        /// The type parameter makes it easier to chain this method to other reactive streams.
        /// </summary>
        /// <typeparam name="T">the desired return type</typeparam>
        /// <returns>an empty reactive stream</returns>
        IObservable<T> Rollback<T>();

        /// <summary>
        /// Gets the transaction configuration.
        /// </summary>
        TransactionConfig TransactionConfig { get; }

        IObservable<T> Close<T>();
    }
}