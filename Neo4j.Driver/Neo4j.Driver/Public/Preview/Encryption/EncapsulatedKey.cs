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
/// An encapsulated data encryption key as stored in an <see cref="IEncapsulatedKeyRepository"/>. This
/// record is part of the Encryption Preview feature and is subject to change or removal.
/// </summary>
/// <param name="Id">The repository-assigned identifier of the key.</param>
/// <param name="Alias">The alias currently bound to this key, if any.</param>
/// <param name="Encapsulation">The encapsulated (wrapped) data encryption key.</param>
/// <param name="Metadata">Metadata persisted alongside the key, e.g. the key encapsulation options.</param>
public record EncapsulatedKey(
    string Id,
    string? Alias,
    byte[] Encapsulation,
    IReadOnlyDictionary<string, string> Metadata);
