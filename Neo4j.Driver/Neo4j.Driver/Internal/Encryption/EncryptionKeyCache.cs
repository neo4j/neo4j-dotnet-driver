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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Neo4j.Driver.Internal.Encryption;

// Phase 1: unbounded, no TTL or size limit (deferred). Keyed by (profile, key id).
[DriverAutoRegister(singleton: true)]
internal class EncryptionKeyCache : IEncryptionKeyCache
{
    private readonly ConcurrentDictionary<(string Profile, string KeyId), byte[]> _cache = new();

    public bool TryGet(string profileName, string keyId, [NotNullWhen(true)] out byte[]? key)
    {
        return _cache.TryGetValue((profileName, keyId), out key);
    }

    public void Set(string profileName, string keyId, byte[] key)
    {
        _cache[(profileName, keyId)] = key;
    }
}
