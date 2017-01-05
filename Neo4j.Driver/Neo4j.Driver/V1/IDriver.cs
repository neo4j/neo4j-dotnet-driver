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

namespace Neo4j.Driver.V1
{
    /// <summary>
    ///     The <see cref="IDriver"/> instance maintains the connections with a Neo4j database, providing an access point via the
    ///     <see cref="Session" /> method.
    /// </summary>
    /// <remarks>
    ///     The Driver maintains a session pool buffering the <see cref="ISession" />s created by the user. 
    ///     The size of the buffer can be configured by the <see cref="Config.MaxIdleSessionPoolSize" /> property on the <see cref="Config" /> when creating the Driver.
    /// </remarks>
    public interface IDriver : IDisposable
    {
        /// <summary>
        ///     Gets the <see cref="Uri" /> of the Neo4j database.
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        ///     Establish a session with Neo4j instance.
        /// </summary>
        /// <returns>
        ///     An <see cref="ISession" /> that could be used to <see cref="IStatementRunner.Run(Statement)" /> a statement or begin a
        ///     transaction.
        /// </returns>
        ISession Session();
    }
}