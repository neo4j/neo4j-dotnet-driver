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

using Neo4j.Driver.Tests.BenchkitBackend.Abstractions;
using Neo4j.Driver.Tests.BenchkitBackend.Types;

namespace Neo4j.Driver.Tests.BenchkitBackend.Implementations;

internal class WorkloadStore : IWorkloadStore
{
    private readonly Dictionary<string, Workload> _workloads = new();

    public string CreateWorkload(Workload workload)
    {
        var id = Guid.NewGuid().ToString("N");
        _workloads[id] = workload;
        return id;
    }

    public Workload GetWorkload(string id)
    {
        if (!_workloads.TryGetValue(id, out var workload))
        {
            throw new KeyNotFoundException($"Workload with id {id} not found.");
        }

        return workload;
    }

    public Workload UpdateWorkload(string id, Workload workload)
    {
        if (!_workloads.ContainsKey(id))
        {
            throw new KeyNotFoundException($"Workload with id {id} not found.");
        }

        _workloads[id] = workload;
        return workload;
    }

    public void DeleteWorkload(string id)
    {
        if (!_workloads.Remove(id))
        {
            throw new KeyNotFoundException($"Workload with id {id} not found.");
        }
    }

    public IDictionary<string, Workload> GetAllWorkloads()
    {
        return _workloads;
    }
}
