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
using System.Diagnostics.CodeAnalysis;
using Neo4j.Driver.Internal.Caching;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Internal.Encryption;

internal class EncryptionKeyCache : IEncryptionKeyCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    private const int CapacityPerProfile = 100;

    private readonly PerProfileBoundedCache<byte[]> _cache;

    public EncryptionKeyCache(IDateTimeProvider clock)
    {
        _cache = new PerProfileBoundedCache<byte[]>(CapacityPerProfile, Ttl, clock);
    }

    public bool TryGet(string profileName, string keyId, [NotNullWhen(true)] out byte[]? key)
    {
        return _cache.TryGet(profileName, keyId, out key);
    }

    public void Set(string profileName, string keyId, byte[] key)
    {
        _cache.Set(profileName, keyId, key);
    }
}
