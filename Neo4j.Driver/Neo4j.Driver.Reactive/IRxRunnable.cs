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

namespace Neo4j.Driver
{
    /// <summary>
    /// Common interface that enables execution of Neo4j statements using Reactive API.
    /// </summary>
    /// 
    /// <seealso cref="IRxSession"/>
    /// <seealso cref="IRxTransaction"/>
    public interface IRxRunnable
    {
        /// <summary>
        /// Create <see cref="IRxResult">a reactive result</see> that will execute the statement.
        /// </summary>
        /// <param name="statement">statement to be executed</param>
        /// <returns>a reactive result</returns>
        ///
        /// <see cref="Run(Statement)"/>
        IRxResult Run(string statement);

        /// <summary>
        /// Create <see cref="IRxResult">a reactive result</see> that will execute the statement
        /// with the specified parameters.
        /// </summary>
        /// <param name="statement">statement to be executed</param>
        /// <param name="parameters">a parameter dictionary, can be an
        ///     <see cref="IDictionary{String,Object}" /> or an anonymous object</param>
        /// <returns>a reactive result</returns>
        ///
        /// <see cref="Run(Statement)"/>
        IRxResult Run(string statement, object parameters);

        /// <summary>
        /// Create <see cref="IRxResult">a reactive result</see> that will execute the given statement.
        ///
        /// The statement is only executed when an <see cref="IObserver{T}"/> is subscribed to one of the
        /// reactive streams that can be accessed through the returned reactive result. 
        /// 
        /// </summary>
        /// <param name="statement">statement to be executed</param>
        /// <returns>a reactive result</returns>
        IRxResult Run(Statement statement);
    }
}