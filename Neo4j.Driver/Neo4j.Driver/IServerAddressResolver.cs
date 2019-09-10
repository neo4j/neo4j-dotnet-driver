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
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    /// <summary>
    /// Used by routing driver to resolve the initial address used to create the driver.
    /// Such resolution happens: 1) during the very first rediscovery when driver is created.
    /// 2) when all the known routers from the current routing table have failed and driver needs to fallback to the initial address.
    /// </summary>
    public interface IServerAddressResolver
    {
        /// <summary>
        /// Given a server address with host name and port defined in <see cref="ServerAddress"/>,
        /// returns the resolved server addresses with host name and port saved in a set of <see cref="ServerAddress"/>.
        /// </summary>
        /// <param name="address">The server address to resolve.</param>
        /// <returns>Resolved server addresses in a set.</returns>
        ISet<ServerAddress> Resolve(ServerAddress address);
    }
    
   
}