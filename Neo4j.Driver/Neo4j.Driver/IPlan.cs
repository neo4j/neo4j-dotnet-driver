//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IPlan
    {
        ///
        /// Return the operation this plan is performing.
        ///
        string OperatorType { get; }

        ///
        /// Many {@link #operatorType() operators} have arguments defining their specific behavior. This map contains
        /// those arguments.
        ///
        /// Return the arguments for the {@link #operatorType() operator} used.
        ///
        IDictionary<string, object> Arguments { get; }

        ///
        /// Identifiers used by this part of the plan. These can be both identifiers introduce by you, or automatically
        /// generated identifiers.
        /// Return a list of identifiers used by this plan.
        ///
        IList<string> Identifiers { get; }

        ///
        /// As noted in the class-level javadoc, a plan is a tree, where each child is another plan. The children are where
        /// this part of the plan gets its input records - unless this is an {@link #operatorType() operator} that introduces
        /// new records on its own.
        /// Return zero or more child plans.
        ///
        IList<IPlan> Children { get; }
    }
}