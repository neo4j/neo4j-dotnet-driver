// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

using System;
using System.Collections.Generic;

namespace Neo4j.Driver;

/// <summary>
/// Represents an <c>Entity</c> in the Neo4j graph database. An <c>Entity</c> could be a <c>Node</c> or a
/// <c>Relationship</c>.
/// </summary>
public interface IEntity
{
    /// <summary>Gets the value that has the specified key in <see cref="Properties" />.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value specified by the given key in <see cref="Properties" />.</returns>
    object this[string key] { get; }

    /// <summary>Gets the properties of the entity.</summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>Get the identity as a <see cref="long" /> number.</summary>
    [Obsolete("Replaced with ElementId, will be removed in 6.0")]
    long Id { get; }

    /// <summary>Get the identity as a <see cref="string" />.</summary>
    string ElementId { get; }
}
