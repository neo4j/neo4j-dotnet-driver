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

using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class PropertyEncryption : IPropertyEncryption
{
    private readonly IEncryptionRequestRunner _runner;
    private readonly IEncryptionProfileRegistry _registry;
    private readonly IEncapsulatedKeyManagerFactory _keyManagerFactory;

    public PropertyEncryption(
        IEncryptionRequestRunner runner,
        IEncryptionProfileRegistry registry,
        IEncapsulatedKeyManagerFactory keyManagerFactory)
    {
        _runner = runner;
        _registry = registry;
        _keyManagerFactory = keyManagerFactory;
    }

    public IEncryptRequestValueStep EncryptRequest()
    {
        return new EncryptRequestBuilder(_runner);
    }

    public IDecryptRequestValueStep DecryptRequest()
    {
        return new DecryptRequestBuilder(_runner);
    }

    public IEncapsulatedKeyManager KeyManager(string? profileName = null)
    {
        return _keyManagerFactory.CreateKeyManager(_registry.Get(profileName));
    }
}
