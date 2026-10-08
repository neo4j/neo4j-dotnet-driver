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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Manages the encapsulated data encryption keys of a specific encryption profile. Obtain an instance
/// via <see cref="IPropertyEncryption.KeyManager(string)"/>. This interface is part of the Encryption
/// Preview feature, and is subject to change or removal.
/// </summary>
public interface IEncapsulatedKeyManager
{
    /// <summary>
    /// Generates a new data encryption key, encapsulates it, and persists it. This method is part of
    /// the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias to bind to the new key, or <see langword="null"/> to create it unaliased.</param>
    /// <param name="encapsulationOptions">
    /// Options for the encapsulation, or <see langword="null"/> to encapsulate with no options.
    /// </param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The created key.</returns>
    /// <exception cref="PropertyEncryptionException">The key could not be created.</exception>
    /// <exception cref="Neo4jException">
    /// The configured <see cref="IKeyEncapsulationService"/> or <see cref="IEncapsulatedKeyRecordRepository"/>
    /// raised a driver exception.
    /// </exception>
    Task<EncapsulatedKey> CreateAsync(
        string? alias = null,
        IKeyEncapsulationOptions? encapsulationOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an encapsulated key by its alias. This method is part of the Encryption Preview feature,
    /// and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias to look up.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The matching key, or <see langword="null"/> if no key has that alias.</returns>
    Task<EncapsulatedKey?> FindByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a key's alias, replacing any alias it already has. This method is part of the Encryption
    /// Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="id">The id of the key to set the alias on.</param>
    /// <param name="alias">The alias to bind, or <see langword="null"/> to remove the key's alias.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task SetAliasByIdAsync(string id, string? alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a key's alias. This method is part of the Encryption Preview feature, and is subject to
    /// change or removal.
    /// </summary>
    /// <param name="id">The id of the key to remove the alias from.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task DeleteAliasByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key and its alias. This method is part of the Encryption Preview feature, and is
    /// subject to change or removal.
    /// </summary>
    /// <param name="id">The id of the key to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
}
