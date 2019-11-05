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
        /// asynchronous <see cref="IAsyncSession"/> with default <see cref="SessionOptions"/>.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <returns>A simple session instance</returns>
        public static ISession Session(this IDriver driver)
        {
            return Session(driver, o => { });
        }

        /// <summary>
        /// Obtain a session which is designed to be used synchronously, which is built on top of the default
        /// asynchronous <see cref="IAsyncSession"/> with the customized <see cref="SessionOptions"/>.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <param name="optionsBuilder">An action, provided with a <see cref="SessionOptions"/> instance, that should populate
        /// the provided instance with desired options.</param> 
        /// <returns>A simple session instance</returns>
        public static ISession Session(this IDriver driver, Action<SessionOptions> optionsBuilder)
        {
            var asyncDriver = driver.CastOrThrow<IInternalDriver>();

            return new InternalSession(driver.AsyncSession(optionsBuilder).CastOrThrow<IInternalAsyncSession>(),
                new RetryLogic(asyncDriver.Config.MaxTransactionRetryTime, asyncDriver.Config.DriverLogger),
                new BlockingExecutor());
        }
    }
}