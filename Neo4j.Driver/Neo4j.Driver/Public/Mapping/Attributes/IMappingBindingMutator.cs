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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Implemented by attributes that customise how the default mapper reads a property or parameter.
/// </summary>
/// <remarks>
/// <para>
/// The built-in mapping attributes (<see cref="MappingSourceAttribute"/>, <see cref="MappingOptionalAttribute"/>,
/// <see cref="MappingDefaultValueAttribute"/>, etc.) all implement this interface.
/// </para>
/// <para>
/// You can implement this interface on your own attribute classes to create reusable, composable mapping
/// customisations. When the default mapper processes a property or parameter, it calls <see cref="Mutate"/>
/// on every <see cref="IMappingBindingMutator"/> attribute it finds, in undefined order, giving each one
/// the opportunity to modify the <see cref="MappingBinding"/>.
/// </para>
/// </remarks>
public interface IMappingBindingMutator
{
    /// <summary>
    /// Called by the default mapper to allow this mutator to update the mapping configuration for a property
    /// or parameter.
    /// </summary>
    /// <param name="binding">The binding to mutate. Modify its properties to change mapping behaviour.</param>
    void Mutate(MappingBinding binding);
}
