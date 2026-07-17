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

/// <summary>The base exception for failures raised by an <see cref="IEncapsulatedKeyRepository"/>.</summary>
public class EncapsulatedKeyRepositoryException(string message) : Neo4jException(message);

/// <summary>Thrown when an <see cref="IEncapsulatedKeyRepository"/> is asked for a key id it doesn't have.</summary>
public class EncapsulatedKeyNotFoundException(string id)
    : EncapsulatedKeyRepositoryException($"Encapsulated key with id '{id}' not found.");

/// <summary>Thrown when an <see cref="IEncapsulatedKeyRepository"/> is asked for an alias it doesn't have.</summary>
public class EncapsulatedAliasNotFoundException(string alias)
    : EncapsulatedKeyRepositoryException($"Alias '{alias}' not found.");
