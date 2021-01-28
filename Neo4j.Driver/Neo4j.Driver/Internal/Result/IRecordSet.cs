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
using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    /// <summary>
    /// A record set represents a set of records where only forward enumeration is possible.
    /// A record is considered consumed when it has been visited by enumeration. 
    /// It will not be available by any other future enumerations.
    /// </summary>
    internal interface IRecordSet
    {
        /// <summary>
        /// Returns true if this set contains no elements to consume.
        /// </summary>
        bool AtEnd { get; }

        /// <summary>
        /// Retrievers the next <see cref="IRecord"/>  without consuming it or returns null if the set is empty.
        /// </summary>
        IRecord Peek();

        /// <summary>
        /// Returns an IEnumerable of records.
        /// </summary>
        IEnumerable<IRecord> Records();
    }
}
