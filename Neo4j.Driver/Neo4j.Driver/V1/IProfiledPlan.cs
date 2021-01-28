﻿// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// This is the same as a regular <see cref="IPlan"/> - except this plan has been executed, meaning it also contains detailed information about how much work each
    /// step of the plan incurred on the database.
    /// </summary>
    public interface IProfiledPlan:IPlan
    {
        /// <summary>
        /// Gets the number of times this part of the plan touched the underlying data stores
        /// </summary>
        long DbHits { get; }

        /// <summary>
        /// Gets the number of records this part of the plan produced
        /// </summary>
        long Records { get; }

        /// <summary>
        /// Gets zero or more child profiled plans.
        /// 
        /// A profiled plan is a tree, where each child is another profiled plan. The children are where
        /// this part of the plan gets its input records - unless this is an <see cref="IPlan.OperatorType"/> that introduces
        /// new records on its own.
        /// </summary>
        new IList<IProfiledPlan> Children { get; }
    }
}
