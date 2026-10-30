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

using System.Diagnostics.CodeAnalysis;
using Neo4j.Driver.Internal.Caching;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal abstract class ProfileScopedCache<TValue>
{
    private readonly PerProfileBoundedCache<TValue> _cache;

    protected ProfileScopedCache(IDateTimeProvider clock)
    {
        _cache = new PerProfileBoundedCache<TValue>(clock);
    }

    protected abstract CacheConfig? ConfigFor(IEnvelopeEncryptionProfile profile);

    public bool TryGet(IEnvelopeEncryptionProfile profile, string key, [NotNullWhen(true)] out TValue? value)
    {
        var config = ConfigFor(profile);
        if (config is null)
        {
            value = default;
            return false;
        }

        return _cache.TryGet(profile.Name, config, key, out value);
    }

    public void Set(IEnvelopeEncryptionProfile profile, string key, TValue value)
    {
        var config = ConfigFor(profile);
        if (config is null)
        {
            return;
        }

        _cache.Set(profile.Name, config, key, value);
    }

    public void Remove(IEnvelopeEncryptionProfile profile, string key)
    {
        _cache.Remove(profile.Name, key);
    }
}
