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
/// Generates and unwraps data encryption keys under a key encryption key managed outside the driver, e.g. by a
/// cloud KMS. Implement this interface to integrate a key management provider with client-side property
/// encryption. This interface is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
public interface IKeyEncapsulationService
{
    /// <summary>
    /// Generates a new data encryption key and encapsulates (wraps) it under the key encryption key.
    /// This method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="options">Options controlling the encapsulation.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The generated key and its encapsulation.</returns>
    Task<EncapsulationResult> EncapsulateAsync(
        IKeyEncapsulationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unwraps a previously encapsulated data encryption key. This method is part of the Encryption
    /// Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="encapsulation">The encapsulated (wrapped) data encryption key.</param>
    /// <param name="options">The options that were persisted alongside the encapsulation when it was created.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The unwrapped plaintext data encryption key.</returns>
    Task<byte[]> DecapsulateAsync(
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> options,
        CancellationToken cancellationToken = default);
}
