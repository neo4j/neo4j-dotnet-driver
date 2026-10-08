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

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// A reference to an encapsulated data encryption key: its repository-assigned id, and its alias if one
/// is bound. This record is part of the Encryption Preview feature and is subject to change or removal.
/// </summary>
/// <param name="Id">The repository-assigned identifier of the key.</param>
/// <param name="Alias">The alias currently bound to this key, if any.</param>
public record EncapsulatedKey(string Id, string? Alias);
