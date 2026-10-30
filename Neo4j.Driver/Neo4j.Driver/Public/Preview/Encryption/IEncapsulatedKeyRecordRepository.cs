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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Stores and retrieves encapsulated data encryption keys, keyed by a repository-assigned id and looked
/// up by id or alias. Implement this interface to back client-side property encryption with your own key
/// store. This interface is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
/// <remarks>
/// A key has at most one alias at a time; binding an alias to a key replaces any alias it previously had.
/// An alias must not be in use by another key: remove it from that key first.
/// </remarks>
public interface IEncapsulatedKeyRecordRepository
{
    /// <summary>
    /// Finds an encapsulated key by its id. This method is part of the Encryption Preview feature, and
    /// is subject to change or removal.
    /// </summary>
    /// <param name="id">The id to look up.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The matching key, or <see langword="null"/> if no key has that id.</returns>
    Task<EncapsulatedKeyRecord?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an encapsulated key by its alias. This method is part of the Encryption Preview feature,
    /// and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias to look up.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The matching key, or <see langword="null"/> if no key has that alias.</returns>
    Task<EncapsulatedKeyRecord?> FindByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a key from the given encapsulation and assigns it a globally unique, immutable id. This
    /// method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias to bind to the new key, or <see langword="null"/> to create it unaliased.</param>
    /// <param name="encapsulation">The encapsulated (wrapped) data encryption key.</param>
    /// <param name="metadata">Metadata to persist alongside the key.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The created key, including its repository-assigned id.</returns>
    /// <exception cref="EncapsulatedAliasInUseException">The alias is already held by another key.</exception>
    Task<EncapsulatedKeyRecord> CreateAsync(
        string? alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a key's alias, replacing any alias it already has. This method is part of the Encryption
    /// Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="id">The id of the key to set the alias on.</param>
    /// <param name="alias">The alias to bind, or <see langword="null"/> to remove the key's alias.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="EncapsulatedKeyNotFoundException">The id is not found.</exception>
    /// <exception cref="EncapsulatedAliasInUseException">The alias is already held by another key.</exception>
    Task SetAliasByIdAsync(string id, string? alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key and its alias. This method is part of the Encryption Preview feature, and is
    /// subject to change or removal.
    /// </summary>
    /// <param name="id">The id of the key to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="EncapsulatedKeyNotFoundException">The id is not found.</exception>
    Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
}
