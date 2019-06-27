// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Linq;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    /// <summary>
    /// Provides extension methods on <see cref="Neo4j.Driver.IDriver"/> for acquiring synchronous
    /// session instances.
    /// </summary>
    public static class DriverExtensions
    {
        /// <summary>
        /// Obtain a session which is designed to be used synchronously, which is built on top of the default
        /// asynchronous <see cref="Neo4j.Driver.ISession"/> with <see cref="AccessMode.Write"/>.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <returns>A reactive session instance</returns>
        public static ISyncSession SyncSession(this IDriver driver)
        {
            return SyncSession(driver, AccessMode.Write);
        }

        /// <summary>
        /// Obtain a session which is designed to be used synchronously, which is built on top of the default
        /// asynchronous <see cref="Neo4j.Driver.ISession"/> with the specified access mode.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <param name="mode">access mode for the returned session</param>
        /// <returns>A reactive session instance</returns>
        public static ISyncSession SyncSession(this IDriver driver, AccessMode mode)
        {
            return SyncSession(driver, mode, Enumerable.Empty<string>());
        }

        /// <summary>
        /// Obtain a session which is designed to be used synchronously, which is built on top of the default
        /// asynchronous <see cref="Neo4j.Driver.ISession"/> with the specified access mode and bookmarks.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <param name="mode">access mode for the returned session</param>
        /// <param name="bookmarks">bookmarks to establish causal chaining</param>
        /// <returns>A reactive session instance</returns>
        public static ISyncSession SyncSession(this IDriver driver, AccessMode mode, IEnumerable<string> bookmarks)
        {
            var reactiveDriver = driver.CastOrThrow<IReactiveDriver>();

            return new InternalSession(driver.Session(mode, bookmarks).CastOrThrow<IReactiveSession>(),
                new SyncRetryLogic(reactiveDriver.Config.MaxTransactionRetryTime, reactiveDriver.Config.DriverLogger),
                new SyncExecutor());
        }
    }
}