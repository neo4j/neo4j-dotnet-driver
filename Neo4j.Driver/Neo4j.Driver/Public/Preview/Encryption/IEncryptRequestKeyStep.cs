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
/// The stage of building an encrypt request where optional additional authenticated data (AAD) and a
/// non-default profile can be supplied, before selecting the data encryption key to encrypt with.
/// This interface is part of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
public interface IEncryptRequestKeyStep
{
    /// <summary>
    /// Sets the additional authenticated data (AAD) to bind to the ciphertext. This method is part
    /// of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="aad">The AAD value.</param>
    /// <returns>This stage, so further optional calls can be chained.</returns>
    IEncryptRequestKeyStep WithAad(object aad);

    /// <summary>
    /// Selects the named encryption profile to encrypt with, instead of the sole configured profile.
    /// This method is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="profileName">The name of the profile to use.</param>
    /// <returns>This stage, so further optional calls can be chained.</returns>
    IEncryptRequestKeyStep UsingProfile(string profileName);

    /// <summary>
    /// Selects the data encryption key to encrypt with by its alias. This method is part of the
    /// Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="alias">The alias of the key.</param>
    /// <returns>The next stage of the request.</returns>
    IEncryptRequestExecuteStep UsingKeyAlias(string alias);

    /// <summary>
    /// Selects the data encryption key to encrypt with by its repository-assigned id. This method
    /// is part of the Encryption Preview feature, and is subject to change or removal.
    /// </summary>
    /// <param name="id">The id of the key.</param>
    /// <returns>The next stage of the request.</returns>
    IEncryptRequestExecuteStep UsingKeyId(string id);
}
