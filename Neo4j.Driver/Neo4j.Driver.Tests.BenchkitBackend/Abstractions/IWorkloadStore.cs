// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Neo4j.Driver.Tests.BenchkitBackend.Types;

namespace Neo4j.Driver.Tests.BenchkitBackend.Abstractions;

/// <summary>
/// Represents a store for managing workloads.
/// </summary>
public interface IWorkloadStore
{
    /// <summary>
    /// Creates a new workload in the store.
    /// </summary>
    /// <param name="workload">The workload to create.</param>
    /// <returns>The ID of the created workload.</returns>
    string CreateWorkload(Workload workload);

    /// <summary>
    /// Retrieves a workload from the store.
    /// </summary>
    /// <param name="id">The ID of the workload to retrieve.</param>
    /// <returns>The retrieved workload.</returns>
    Workload GetWorkload(string id);

    /// <summary>
    /// Updates a workload in the store.
    /// </summary>
    /// <param name="id">The ID of the workload to update.</param>
    /// <param name="workload">The updated workload.</param>
    /// <returns>The updated workload.</returns>
    Workload UpdateWorkload(string id, Workload workload);

    /// <summary>
    /// Deletes a workload from the store.
    /// </summary>
    /// <param name="id">The ID of the workload to delete.</param>
    void DeleteWorkload(string id);

    /// <summary>
    /// Retrieves the full list of workloads from the store.
    /// </summary>
    IDictionary<string, Workload> GetAllWorkloads();
}
