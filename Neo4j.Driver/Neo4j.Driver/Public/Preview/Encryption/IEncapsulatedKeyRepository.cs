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
/// Stores and retrieves encapsulated data encryption keys, keyed by a repository-assigned id and looked up by
/// id or alias. Implement this interface to back client-side property encryption with your own key store.
/// This interface is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
/// <remarks>
/// Every method throws if it cannot do what its name says: an unknown id or alias throws rather than returning
/// null or silently doing nothing. A key has at most one alias at a time; binding a new alias to a key replaces
/// any alias it previously had.
/// </remarks>
public interface IEncapsulatedKeyRepository
{
    /// <summary>
    /// Finds an encapsulated key by id or alias. This method is part of the Encryption Preview
    /// feature, and is subject to change or removal.
    /// </summary>
    /// <param name="keyReference">The id or alias to look up.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The matching encapsulated key.</returns>
    /// <exception cref="EncapsulatedKeyNotFoundException">The id is not found.</exception>
    /// <exception cref="EncapsulatedAliasNotFoundException">The alias is not found.</exception>
    Task<EncapsulatedKey> FindAsync(KeyReference keyReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new encapsulated key, optionally under an alias. This method is part of the
    /// Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias to bind to the new key, or <see langword="null"/> to save it unaliased.</param>
    /// <param name="encapsulation">The encapsulated (wrapped) data encryption key.</param>
    /// <param name="metadata">Metadata to persist alongside the key.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The stored key, including its repository-assigned id.</returns>
    Task<EncapsulatedKey> SaveAsync(
        string? alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds an alias to an existing key, replacing any alias it already has and moving the alias from any key
    /// it was previously bound to. This method is part of the Encryption Preview feature, and is subject to
    /// change or removal.
    /// </summary>
    /// <param name="id">The id of the key to bind the alias to.</param>
    /// <param name="alias">The alias to bind.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="EncapsulatedKeyNotFoundException">The id is not found.</exception>
    Task AddAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a key's alias. This method is part of the Encryption Preview feature, and is subject
    /// to change or removal.
    /// </summary>
    /// <param name="id">The id of the key to remove the alias from.</param>
    /// <param name="alias">The alias to remove.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="EncapsulatedKeyNotFoundException">The id is not found.</exception>
    /// <exception cref="EncapsulatedAliasNotFoundException">The alias is not bound to the key.</exception>
    Task DeleteAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key and its alias. This method is part of the Encryption Preview feature, and is
    /// subject to change or removal.
    /// </summary>
    /// <param name="id">The id of the key to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="EncapsulatedKeyNotFoundException">The id is not found.</exception>
    Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
}
