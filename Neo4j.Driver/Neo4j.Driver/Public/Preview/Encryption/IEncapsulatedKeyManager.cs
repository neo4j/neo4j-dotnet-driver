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
/// Creates new encapsulated data encryption keys for a specific encryption profile. Obtain an instance via
/// <see cref="IPropertyEncryption.KeyManager(string)"/>. This interface is part of the Encryption Preview
/// feature, and is subject to change or removal.
/// </summary>
public interface IEncapsulatedKeyManager
{
    /// <summary>
    /// Generates a new data encryption key, encapsulates it, and persists it under the given alias.
    /// This method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias to bind to the new key.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The stored key.</returns>
    /// <exception cref="PropertyEncryptionException">The key could not be created.</exception>
    /// <exception cref="Neo4jException">
    /// The configured <see cref="IKeyEncapsulationService"/> or <see cref="IEncapsulatedKeyRepository"/>
    /// raised a driver exception.
    /// </exception>
    Task<EncapsulatedKey> CreateAsync(string alias, CancellationToken cancellationToken = default);
}
