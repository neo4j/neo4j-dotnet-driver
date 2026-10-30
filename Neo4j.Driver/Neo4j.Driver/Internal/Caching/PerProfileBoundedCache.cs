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
using System.Diagnostics.CodeAnalysis;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Caching;

internal class PerProfileBoundedCache<TValue>
{
    private readonly IDateTimeProvider _clock;
    private readonly object _lock = new();
    private readonly Dictionary<string, IBoundedCache<string, TValue>> _perProfile = new();

    public PerProfileBoundedCache(IDateTimeProvider clock)
    {
        _clock = clock;
    }

    public bool TryGet(string profileName, CacheConfig config, string key, [NotNullWhen(true)] out TValue? value)
    {
        return GetOrAddProfileCache(profileName, config).TryGet(key, out value);
    }

    public void Set(string profileName, CacheConfig config, string key, TValue value)
    {
        GetOrAddProfileCache(profileName, config).Set(key, value);
    }

    public void Remove(string profileName, string key)
    {
        IBoundedCache<string, TValue>? cache;
        lock (_lock)
        {
            _perProfile.TryGetValue(profileName, out cache);
        }

        cache?.Remove(key);
    }

    private IBoundedCache<string, TValue> GetOrAddProfileCache(string profileName, CacheConfig config)
    {
        lock (_lock)
        {
            if (_perProfile.TryGetValue(profileName, out var cache))
            {
                return cache;
            }

            cache = new BoundedLruCache<string, TValue>(config.MaxSize, config.Ttl, _clock);
            _perProfile[profileName] = cache;

            return cache;
        }
    }
}
