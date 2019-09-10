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
    /// Provides extension methods on <see cref="Neo4j.Driver.IDriver"/> for acquiring reactive
    /// session instance.
    /// </summary>
    public static class DriverExtensions
    {
        /// <summary>
        /// Obtain a session which is designed to be used through <see cref="System.Reactive"/> with
        /// access mode <see cref="AccessMode.Write"/>.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <returns>A reactive session instance</returns>
        public static IRxSession RxSession(this IDriver driver)
        {
            return RxSession(driver, o => { });
        }

        /// <summary>
        /// Obtain a session which is designed to be used through <see cref="System.Reactive"/> with
        /// the specified access mode.
        /// </summary>
        /// <param name="driver">driver instance</param>
        /// <param name="optionsBuilder">An action, provided with a <see cref="SessionOptions"/> instance, that should populate
        /// the provided instance with desired options.</param> 
        /// <returns>A reactive session instance</returns>
        public static IRxSession RxSession(this IDriver driver, Action<SessionOptions> optionsBuilder)
        {
            var reactiveDriver = driver.CastOrThrow<IInternalDriver>();

            return new InternalRxSession(reactiveDriver.Session(optionsBuilder, true),
                new RxRetryLogic(reactiveDriver.Config.MaxTransactionRetryTime, reactiveDriver.Config.DriverLogger));
        }
    }
}