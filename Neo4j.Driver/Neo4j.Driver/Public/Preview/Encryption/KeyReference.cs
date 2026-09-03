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
/// Distinguishes whether a <see cref="KeyReference"/> refers to a key by its repository-assigned id
/// or by an alias. This enum is part of the Encryption Preview feature, and is subject to change or
/// removal.
/// </summary>
public enum KeyReferenceType
{
    /// <summary>
    /// The reference is a repository-assigned key id. This value is part of the Encryption Preview
    /// feature, and is subject to change or removal.
    /// </summary>
    Id,

    /// <summary>
    /// The reference is an alias bound to a key. This value is part of the Encryption Preview
    /// feature, and is subject to change or removal.
    /// </summary>
    Alias
}

/// <summary>
/// A reference to an encapsulated key, either by id or by alias. This record is part of the
/// Encryption Preview feature, and is subject to change or removal.
/// </summary>
/// <param name="Reference">The id or alias value.</param>
/// <param name="Type">Which kind of reference <paramref name="Reference"/> is.</param>
public record KeyReference(string Reference, KeyReferenceType Type);
