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

using System;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Factory methods for the <see cref="IKeyEncapsulationService"/> implementations supplied by the
/// driver. This class is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
public static class KeyEncapsulationServices
{
    /// <summary>
    /// Creates a service that wraps and unwraps data encryption keys with a local AES-256 key
    /// encryption key held in memory. This method is part of the Encryption Preview feature, and is
    /// subject to change or removal.
    /// </summary>
    /// <param name="masterKey">The AES-256 key encryption key, which must be 32 bytes long.</param>
    /// <returns>The configured service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="masterKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="masterKey"/> is not 32 bytes long.</exception>
    public static IKeyEncapsulationService Local(byte[] masterKey)
    {
        ArgumentNullException.ThrowIfNull(masterKey);
        if (masterKey.Length != AesGcmConstants.KeyLengthInBytes)
        {
            throw new ArgumentException(
                $"An AES-256 master key must be {AesGcmConstants.KeyLengthInBytes} bytes long, but was {masterKey.Length}.",
                nameof(masterKey));
        }

        return new LocalKeyEncapsulationService(
            [..masterKey],
            new AesGcmCipher(),
            new CryptoRandomProvider(),
            new Base64Codec());
    }
}
