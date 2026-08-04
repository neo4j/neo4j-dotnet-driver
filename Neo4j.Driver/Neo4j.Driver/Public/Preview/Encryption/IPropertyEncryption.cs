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
/// The entry point for client-side property encryption. Obtain an instance via
/// <c>driver.PropertyEncryption()</c>. This interface is part of the Encryption Preview feature,
/// and is subject to change or removal.
/// </summary>
public interface IPropertyEncryption
{
    /// <summary>
    /// Begins building a request to encrypt a value. This method is part of the Encryption Preview
    /// feature, and is subject to change or removal.
    /// </summary>
    /// <returns>The first stage of the request.</returns>
    IEncryptRequestValueStep EncryptRequest();

    /// <summary>
    /// Begins building a request to decrypt a value. This method is part of the Encryption Preview
    /// feature, and is subject to change or removal.
    /// </summary>
    /// <returns>The first stage of the request.</returns>
    IDecryptRequestValueStep DecryptRequest();

    /// <summary>
    /// Returns the key manager for the sole configured encryption profile. This method is part of
    /// the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <returns>The key manager.</returns>
    /// <exception cref="DefaultEncryptionProfileNotFoundException">No encryption profile is configured.</exception>
    /// <exception cref="AmbiguousEncryptionProfileException">More than one encryption profile is configured.</exception>
    /// <exception cref="EncapsulatedKeyManagerNotFoundException">No key manager provider accepts the configured profile.</exception>
    IEncapsulatedKeyManager KeyManager();

    /// <summary>
    /// Returns the key manager for the named encryption profile. This method is part of the
    /// Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <returns>The key manager.</returns>
    /// <exception cref="EncryptionProfileNotFoundException">No profile named <paramref name="profileName"/> is configured.</exception>
    /// <exception cref="EncapsulatedKeyManagerNotFoundException">No key manager provider accepts the profile.</exception>
    IEncapsulatedKeyManager KeyManager(string profileName);
}
