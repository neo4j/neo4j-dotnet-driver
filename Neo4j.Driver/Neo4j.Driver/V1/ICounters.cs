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
namespace Neo4j.Driver
{
    /// <summary>
    /// Represents the changes to the database made as a result of a statement being run.
    /// </summary>
    public interface ICounters
    {
        /// <summary>
        /// Gets whether there were any updates at all, eg. any of the counters are greater than 0.
        /// </summary>
        /// <value>Returns <c>true</c> if the statement made any updates, <c>false</c> otherwise.</value>
        bool ContainsUpdates { get; }

        /// <summary>
        /// Gets the number of nodes created.
        /// </summary>
        int NodesCreated { get; }

        /// <summary>
        /// Gets the number of nodes deleted.
        /// </summary>
        int NodesDeleted { get; }

        /// <summary>
        /// Gets the number of relationships created.
        /// </summary>
        int RelationshipsCreated { get; }

        /// <summary>
        /// Gets the number of relationships deleted.
        /// </summary>
        int RelationshipsDeleted { get; }

        /// <summary>
        /// Gets the number of properties (on both nodes and relationships) set.
        /// </summary>
        int PropertiesSet { get; }

        /// <summary>
        /// Gets the number of labels added to nodes.
        /// </summary>
        int LabelsAdded { get; }

        /// <summary>
        /// Gets the number of labels removed from nodes.
        /// </summary>
        int LabelsRemoved { get; }

        /// <summary>
        /// Gets the number of indexes added to the schema.
        /// </summary>
        int IndexesAdded { get; }

        /// <summary>
        /// Gets the number of indexes removed from the schema.
        /// </summary>
        int IndexesRemoved { get; }

        /// <summary>
        /// Gets the number of constraints added to the schema.
        /// </summary>
        int ConstraintsAdded { get; }

        /// <summary>
        /// Gets the number of constraints removed from the schema.
        /// </summary>
        int ConstraintsRemoved { get; }
    }
}