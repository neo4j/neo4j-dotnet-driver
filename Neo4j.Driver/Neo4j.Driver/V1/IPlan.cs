// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
    /// This describes the <c>Plan</c> that the database planner produced and used (or will use) to execute your statement.
    /// This can be extremely helpful in understanding what a statement is doing, and how to optimize it. For more
    /// details, see the Neo4j Manual.
    ///
    /// The plan for the statement is a tree of plans - each sub-tree containing zero or more child plans. The statement
    /// starts with the root plan. Each sub-plan is of a specific <see cref="OperatorType"/>, which describes
    /// what that part of the plan does - for instance, perform an index lookup or filter results. The Neo4j Manual contains
    /// a reference of the available operator types, and these may differ across Neo4j versions.
    ///
    /// </summary>
    public interface IPlan
    {
        /// <summary>
        /// Gets the operation this plan is performing.
        /// </summary>
        string OperatorType { get; }

        /// <summary>
        /// Gets the arguments for the <see cref="OperatorType"/> used.
        /// 
        /// Many <see cref="OperatorType"/> have arguments defining their specific behavior. This map contains
        /// those arguments.
        /// </summary>
        IDictionary<string, object> Arguments { get; }

        /// <summary>
        /// Gets a list of identifiers used by this plan.
        /// 
        /// Identifiers used by this part of the plan. These can be both identifiers introduce by you, or automatically
        /// generated identifiers.
        ///
        /// </summary>
        IList<string> Identifiers { get; }

        /// <summary>
        /// Gets zero or more child plans.
        /// 
        /// A plan is a tree, where each child is another plan. The children are where
        /// this part of the plan gets its input records - unless this is an <see cref="OperatorType"/> that introduces
        /// new records on its own.
        /// </summary>
        IList<IPlan> Children { get; }
    }
}
