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

using Neo4j.Driver.Internal.Mapping;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Defines an interface that provides metadata for object mapping used in the Neo4j driver.
/// This interface allows access to the <see cref="MappingBinding"/> object, which encapsulates
/// details about the mapping configuration for an object. This interface is intended to be
/// implemented by attributes decorating a property or parameter.
/// </summary>
public interface IMappingBindingMutator
{
    /// <summary>
    /// Modifies the provided <see cref="MappingBinding"/> instance to update or transform
    /// its metadata configuration. This method is  used to define or adjust how
    /// object mapping is performed in the context of the Neo4j driver.
    /// </summary>
    /// <param name="binding">The <see cref="MappingBinding"/> instance to be mutated.</param>
    void Mutate(MappingBinding binding);
}
