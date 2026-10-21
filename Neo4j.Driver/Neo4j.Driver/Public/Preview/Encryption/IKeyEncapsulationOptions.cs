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

#nullable enable

using System.Collections.Generic;

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Options passed to <see cref="IKeyEncapsulationService.EncapsulateAsync"/> controlling how a data encryption key
/// is encapsulated. This interface is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
public interface IKeyEncapsulationOptions
{
    /// <summary>
    /// Converts the options to a flat string-to-string map, e.g. for persistence alongside the
    /// encapsulation. This method is part of the Encryption Preview feature, and is subject to
    /// change or removal.
    /// </summary>
    /// <returns>The options as a read-only map.</returns>
    IReadOnlyDictionary<string, string> ToMap();
}
