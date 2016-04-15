// Copyright (c) 2002-2016 "Neo Technology,"
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

namespace Neo4j.Driver.Internal.Result
{
    /// <summary>
    /// A record set represents a set of records where only forward enumeration is possible.
    /// This means that when a record has been visited by enumeration, then it will not be any
    /// future enumerations. It is consumed.
    /// </summary>
    internal interface IRecordSet
    {
        /// <summary>
        /// Has all records been consumed.
        /// </summary>
        bool AtEnd { get; }

        /// <summary>
        /// Peeks a record without consuming. 
        /// If all records has been consumed, this is null
        /// </summary>
        IRecord Peek();

        /// <summary>
        /// Returns an IEnumerable of records.
        /// </summary>
        IEnumerable<IRecord> Records();
    }
}
