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
/// <c>driver.PropertyEncryption()</c>. This type is part of the encryption preview and is
/// subject to change or removal.
/// </summary>
public interface IPropertyEncryption
{
    /// <summary>Begins building a request to encrypt a value.</summary>
    /// <returns>The first stage of the request.</returns>
    IEncryptRequestValueStep EncryptRequest();

    /// <summary>Begins building a request to decrypt a value.</summary>
    /// <returns>The first stage of the request.</returns>
    IDecryptRequestValueStep DecryptRequest();
}
