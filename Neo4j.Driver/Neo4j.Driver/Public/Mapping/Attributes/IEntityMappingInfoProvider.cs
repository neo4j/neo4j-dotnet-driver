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
/// Defines an interface that provides metadata for entity mapping used in the Neo4j driver.
/// This interface allows access to the <see cref="EntityMappingInfo"/> object, which encapsulates
/// details about the mapping configuration for an entity. This interface is intended to be
/// implemented by attributes decorating a property or parameter.
/// </summary>
public interface IEntityMappingInfoProvider
{
    /// <summary>
    /// Encapsulates metadata information necessary for mapping an entity within the Neo4j driver.
    /// This class holds details such as the mapping path, source, whether the mapping is optional,
    /// the default value to use if the mapping is not present, and whether the mapping is explicit.
    /// </summary>
    EntityMappingInfo EntityMappingInfo { get; }
}
